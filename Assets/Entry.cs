
using System;
using System.Text.RegularExpressions;

using UnityEngine;

public class Entry
{
	public string header;
	public string callstack;
	public string filename;
	public LogType logType;

	public byte[] ToByteArray()
	{
		byte[] headerBytes = Convert.FromBase64String(header);
		byte[] callstackBytes = Convert.FromBase64String(callstack);
		byte[] filenameBytes = Convert.FromBase64String(filename);

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

	public static Entry FromByteArray(byte[] data)
	{
		if (data.Length < 4) return null;

		Entry e = new Entry();
		e.logType = (LogType)data[0];
		if (data[1] > 0)
		{
			e.header = Convert.ToBase64String(data, 4, data[1]);
		}
		if (data[2] > 0)
		{
			e.callstack = Convert.ToBase64String(data, 4 + data[1], data[2]);
		}
		if (data[3] > 0)
		{
			e.filename = Convert.ToBase64String(data, 4 + data[1] + data[2], data[3]);
		}

		return e;
	}

	public Entry FigureOutLogType()
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

