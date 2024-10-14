using UnityEngine;
using System.IO;
using System;

public static class SaveWav
{
    const int HEADER_SIZE = 44;

    public static bool Save(string filepath, AudioClip clip, float minThreshold = 0.01f)
    {
        if (!filepath.ToLower().EndsWith(".wav"))
        {
            filepath += ".wav";
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

        // 무음 부분을 제거합니다.
        AudioClip trimmedClip = TrimSilence(clip, minThreshold);

        if (trimmedClip == null)
        {
            Debug.LogWarning("저장할 오디오가 모두 무음입니다.");
            return false;
        }

        using (var fileStream = CreateEmpty(filepath))
        {
            ConvertAndWrite(fileStream, trimmedClip);
            WriteHeader(fileStream, trimmedClip);
        }

        return true;
    }

    // TrimSilence 메서드를 추가합니다.
    static AudioClip TrimSilence(AudioClip clip, float minThreshold)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        int startIndex = 0;
        int endIndex = samples.Length - 1;

        // 시작 인덱스 찾기
        for (int i = 0; i < samples.Length; i++)
        {
            if (Mathf.Abs(samples[i]) > minThreshold)
            {
                startIndex = i;
                break;
            }
        }

        // 종료 인덱스 찾기
        for (int i = samples.Length - 1; i >= 0; i--)
        {
            if (Mathf.Abs(samples[i]) > minThreshold)
            {
                endIndex = i;
                break;
            }
        }

        int trimmedLength = endIndex - startIndex + 1;

        if (trimmedLength <= 0)
        {
            // 모두 무음인 경우
            return null;
        }

        float[] trimmedSamples = new float[trimmedLength];
        Array.Copy(samples, startIndex, trimmedSamples, 0, trimmedLength);

        AudioClip trimmedClip = AudioClip.Create(clip.name + "_trimmed", trimmedLength / clip.channels, clip.channels, clip.frequency, false);
        trimmedClip.SetData(trimmedSamples, 0);

        return trimmedClip;
    }

    static FileStream CreateEmpty(string filepath)
    {
        var fileStream = new FileStream(filepath, FileMode.Create);
        for (int i = 0; i < HEADER_SIZE; i++)
        {
            fileStream.WriteByte(0);
        }
        return fileStream;
    }

    static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        Int16[] intData = new Int16[samples.Length];
        Byte[] bytesData = new Byte[samples.Length * 2];

        int rescaleFactor = 32767;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * rescaleFactor);
            Byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    static void WriteHeader(FileStream fileStream, AudioClip clip)
    {
        int hz = clip.frequency;
        int channels = clip.channels;
        int samples = clip.samples;

        fileStream.Seek(0, SeekOrigin.Begin);

        Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        Byte[] subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 audioFormat = 1;
        fileStream.Write(BitConverter.GetBytes(audioFormat), 0, 2);

        UInt16 numChannels = (ushort)channels;
        fileStream.Write(BitConverter.GetBytes(numChannels), 0, 2);

        Byte[] sampleRate = BitConverter.GetBytes(hz);
        fileStream.Write(sampleRate, 0, 4);

        Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
        fileStream.Write(byteRate, 0, 4);

        UInt16 blockAlign = (ushort)(channels * 2);
        fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

        UInt16 bitsPerSample = 16;
        fileStream.Write(BitConverter.GetBytes(bitsPerSample), 0, 2);

        Byte[] dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
        fileStream.Write(subChunk2, 0, 4);
    }
}
