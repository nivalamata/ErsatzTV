﻿using ErsatzTV.FFmpeg.Capabilities;
using ErsatzTV.FFmpeg.Format;

namespace ErsatzTV.FFmpeg.GlobalOption.HardwareAcceleration;

public class QsvHardwareAccelerationOption(Option<string> device, FFmpegCapability decodeCapability) : GlobalOption
{
    // TODO: read this from ffmpeg output
    private readonly List<string> _supportedFFmpegFormats = new()
    {
        FFmpegFormat.NV12,
        FFmpegFormat.P010LE
    };

    public override string[] GlobalOptions
    {
        get
        {
            string[] initDevices = OperatingSystem.IsWindows()
                ? new[] { "-init_hw_device", "d3d11va=hw:,vendor=0x8086", "-filter_hw_device", "hw" }
                : new[] { "-init_hw_device", "qsv=hw", "-filter_hw_device", "hw" };

            var result = new List<string>
            {
                "-hwaccel", "qsv",
                "-hwaccel_output_format", "qsv"
            };

            if (decodeCapability is not FFmpegCapability.Hardware)
            {
                result.Clear();
            }

            if (OperatingSystem.IsLinux())
            {
                foreach (string qsvDevice in device)
                {
                    if (!string.IsNullOrWhiteSpace(qsvDevice))
                    {
                        result.AddRange(new[] { "-qsv_device", qsvDevice });
                    }
                }
            }

            result.AddRange(initDevices);

            return result.ToArray();
        }
    }

    // qsv encoders want nv12
    public override FrameState NextState(FrameState currentState)
    {
        FrameState result = currentState;

        foreach (IPixelFormat pixelFormat in currentState.PixelFormat)
        {
            if (_supportedFFmpegFormats.Contains(pixelFormat.FFmpegName))
            {
                return result;
            }

            return result with { PixelFormat = new PixelFormatNv12(pixelFormat.Name) };
        }

        return result with { PixelFormat = new PixelFormatNv12(new PixelFormatUnknown().Name) };
    }
}
