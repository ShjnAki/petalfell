#!/usr/bin/env python3
"""Rebuild weather playback assets from preserved attributed recordings. Requires ffmpeg, numpy."""
from pathlib import Path
import subprocess
import numpy as np
ROOT = Path(__file__).resolve().parents[1] / 'assets/audio/weather'
RATE = 44100

def decode(name, start=0, length=None, filters='anull'):
    args=['ffmpeg','-v','error','-ss',str(start),'-i',str(ROOT/'source'/name)]
    if length: args += ['-t',str(length)]
    args += ['-af',filters,'-f','f32le','-ar',str(RATE),'-ac','2','pipe:1']
    return np.frombuffer(subprocess.check_output(args),dtype='<f4').reshape(-1,2).copy()

def save(name, data):
    # Preserve stereo detail; leave ample room for summing rain and thunder.
    peak=float(np.max(np.abs(data)))
    assert np.isfinite(data).all() and peak > .001
    data *= .78 / peak
    subprocess.run(['ffmpeg','-y','-v','error','-f','f32le','-ar',str(RATE),'-ac','2',
        '-i','pipe:0','-c:a','libvorbis','-q:a','5',str(ROOT/name)], input=data.astype('<f4').tobytes(),check=True)
    print(name, f'{len(data)/RATE:.2f}s, peak={np.max(np.abs(data)):.3f}')

rain=decode('rain-recording.ogg',30,88,'highpass=f=95,lowpass=f=12500')
n=RATE*8
# A circular 8-second equal-power splice: head and tail overlap, both joins
# retain the recording's waveform continuity. No stop/fade/restart at runtime.
t=np.linspace(0,np.pi/2,n,endpoint=True)[:,None]
splice=rain[-n:]*np.cos(t)+rain[:n]*np.sin(t)
loop=np.concatenate([splice,rain[n:-n]])
save('rain-loop.ogg',loop)
thunder=decode('thunderclap.ogg',filters='highpass=f=32,lowpass=f=6500')
a=min(len(thunder)//4,int(RATE*.04));b=min(len(thunder)//3,RATE)
thunder[:a]*=np.linspace(0,1,a)[:,None]
thunder[-b:]*=np.linspace(1,0,b)[:,None]
save('thunder.ogg',thunder)
