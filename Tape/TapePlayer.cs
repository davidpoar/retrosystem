using System;
using System.Collections.Generic;
using Tape.Blocks;

namespace Tape;

/// <summary>
/// Simulates the play-head of a tape deck.
/// Maintains a queue of pulse durations (in T-states / CPU clock cycles) and
/// exposes the current signal level so an emulator can read the cassette input bit
/// on every CPU cycle without knowing the details of the tape format.
///
/// Usage:
/// 1. Construct with the CPU clock frequency of the target machine.
/// 2. Call <see cref="Insert"/> with a loaded <see cref="TapeFile"/>.
/// 3. Call <see cref="Play"/>.
/// 4. On every frame, advance the player with <see cref="AdvanceCycles"/>.
/// 5. Read <see cref="CurrentSignalLevel"/> when the CPU reads the cassette port.
/// </summary>
public class TapePlayer
{
    private readonly int _cpuClockHz;
    private Queue<long>? _pulseQueue;
    private long _cyclesUntilNextToggle;
    private bool _signalLevel;
    private bool _playing;

    /// <summary>
    /// True if the signal line is currently high (logic 1).
    /// Map this to the appropriate input bit of the target machine's cassette port.
    /// </summary>
    public bool CurrentSignalLevel => _signalLevel;

    /// <summary>True while the tape is playing.</summary>
    public bool IsPlaying => _playing;

    /// <summary>True if the tape is inserted and has remaining pulses.</summary>
    public bool HasData => _pulseQueue is { Count: > 0 };

    /// <summary>
    /// Creates a new tape player.
    /// </summary>
    /// <param name="cpuClockHz">CPU clock frequency in Hz (e.g. 3_500_000 for Spectrum, 3_579_545 for MSX).</param>
    public TapePlayer(int cpuClockHz)
    {
        _cpuClockHz = cpuClockHz;
    }

    /// <summary>Insert a tape file and rewind to the beginning.</summary>
    public void Insert(TapeFile tape)
    {
        _pulseQueue = new Queue<long>();
        foreach (var block in tape.Blocks)
        {
            foreach (var pulse in block.GeneratePulses(_cpuClockHz))
            {
                _pulseQueue.Enqueue(pulse);
            }
        }
        _signalLevel = false;
        _cyclesUntilNextToggle = _pulseQueue.Count > 0 ? _pulseQueue.Dequeue() : 0;
        _playing = false;
    }

    /// <summary>Start playback.</summary>
    public void Play() => _playing = true;

    /// <summary>Pause playback. The signal level is frozen.</summary>
    public void Pause() => _playing = false;

    /// <summary>Force the cassette signal to low (cable unplugged / motor off).
    /// Use this when the program turns the cassette motor off so polling routines
    /// that read the cassette port see a clean low signal.</summary>
    public void ForceSignalLow()
    {
        _signalLevel = false;
        _cyclesUntilNextToggle = 0;
    }

    /// <summary>Eject the tape. Playback stops and the signal returns low.</summary>
    public void Eject()
    {
        _pulseQueue = null;
        _playing = false;
        _signalLevel = false;
        _cyclesUntilNextToggle = 0;
    }

    /// <summary>
    /// Advance the tape by the given number of CPU cycles.
    /// Call this once per emulation step (after <see cref="Z80.Z80.Step"/> returns).
    /// </summary>
    public void AdvanceCycles(long cycles)
    {
        if (!_playing || _pulseQueue == null) return;

        _cyclesUntilNextToggle -= cycles;

        // Toggle the signal level for each pulse boundary we've crossed
        while (_cyclesUntilNextToggle <= 0)
        {
            _signalLevel = !_signalLevel;
            if (_pulseQueue.Count > 0)
                _cyclesUntilNextToggle += _pulseQueue.Dequeue();
            else
            {
                // End of tape
                _playing = false;
                _signalLevel = false;
                _cyclesUntilNextToggle = 0;
                return;
            }
        }
    }
}
