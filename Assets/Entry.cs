
using System;
using System.Text;
using System.Text.RegularExpressions;

using UnityEngine;




public struct DebugLogEntry
{
	public string header;
	public string callstack;
	public string filename;
	public LogType logType;

	public bool IsNullOrEmpty() => string.IsNullOrEmpty(header);


	public byte[] ToByteArray()
	{
		byte[] headerBytes = Encoding.UTF32.GetBytes(header);
		byte[] callstackBytes = Encoding.UTF32.GetBytes(callstack);
		byte[] filenameBytes = Encoding.UTF32.GetBytes(filename);

		byte[] data = new byte[1 + 3 + headerBytes.Length + callstackBytes.Length + filenameBytes.Length];

		data[0] = (byte)logType;
		data[1] = (byte)headerBytes.Length;
		data[2] = (byte)callstackBytes.Length;
		data[3] = (byte)filenameBytes.Length;

		for (int i = 0; i < headerBytes.Length; i++)
		{
			data[4 + i] = headerBytes[i];
		}
		for (int i = 0; i < callstackBytes.Length; i++)
		{
			data[4 + headerBytes.Length + i] = callstackBytes[i];
		}
		for (int i = 0; i < filenameBytes.Length; i++)
		{
			data[4 + headerBytes.Length + callstackBytes.Length + i] = filenameBytes[i];
		}

		return data;
	}

	public static DebugLogEntry FromByteArray(byte[] data, int offset, int length)
	{
		if (data.Length < 4) return default;

		DebugLogEntry e = new DebugLogEntry();
		e.logType = (LogType)data[offset];

		byte sHeader = data[offset + 1];
		byte sCallstack = data[offset + 2];
		byte sFilename = data[offset + 3];

		if (length < sHeader + sCallstack + sFilename + 4)
		{
			Debug.LogError($"[DebugLogEntry] invalid data. (data size doesn't match expected)");
			return default;
		}

		if (sHeader > 0)
		{
			e.header = Encoding.UTF32.GetString(data, offset + 4, sHeader);
		}

		if (data[offset + 2] > 0)
		{
			e.callstack = Encoding.UTF32.GetString(data, offset + 4 + sHeader, sCallstack);
		}
		if (sFilename > 0)
		{
			e.filename = Encoding.UTF32.GetString(data, offset + 4 + sHeader + sCallstack, sFilename);
		}

		return e;
	}

	public DebugLogEntry FigureOutLogType()
	{
		logType = LogType.Log;

		if (Regex.IsMatch(header, @"^\S+?[eE]xception"))
		{
			logType = LogType.Exception;
		}
		else if (Regex.IsMatch(header, @"^The referenced script.+?is missing!"))
		{
			logType = LogType.Warning;
		}
		else if (!string.IsNullOrWhiteSpace(callstack))
		{
			if (callstack.Contains("UnityEngine.Debug:LogError("))
			{
				logType = LogType.Error;
			}
			else if (callstack.Contains("UnityEngine.Debug:LogWarning("))
			{
				logType = LogType.Warning;
			}
			else if (callstack.Contains("UnityEngine.Debug:LogAssertion("))
			{
				logType = LogType.Assert;
			}
		}

		return this;
	}
}

