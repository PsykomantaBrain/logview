
using System;
using System.IO;
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

	public DebugLogEntry Write(Stream stream)
	{
		using (BinaryWriter writer = new BinaryWriter(stream))
		{
			byte[] bHeader = Encoding.UTF8.GetBytes(header);
			byte[] bCallstack = Encoding.UTF8.GetBytes(callstack);
			byte[] bFilename = Encoding.UTF8.GetBytes(filename);
			int type = (int)logType;

			writer.Write(type);

			writer.Write(bHeader.Length);
			writer.Write(bHeader);

			writer.Write(bCallstack.Length);
			writer.Write(bCallstack);

			writer.Write(bFilename.Length);
			writer.Write(bFilename);

			writer.Close();
		}
		return this;
	}
	public static DebugLogEntry Read(Stream stream)
	{
		using (BinaryReader reader = new BinaryReader(stream))
		{
			DebugLogEntry e = new DebugLogEntry();

			e.logType = (LogType)reader.ReadInt32();

			int lHeader = reader.ReadInt32();
			if (lHeader > 0)
			{
				byte[] bHeader = reader.ReadBytes(lHeader);
				e.header = Encoding.UTF8.GetString(bHeader, 0, lHeader);
			}
			int lcallstack = reader.ReadInt32();
			if (lcallstack > 0)
			{
				byte[] bcallstack = reader.ReadBytes(lcallstack);
				e.callstack = Encoding.UTF8.GetString(bcallstack, 0, lcallstack);
			}
			int lfile = reader.ReadInt32();
			if (lfile > 0)
			{
				byte[] bfile = reader.ReadBytes(lfile);
				e.filename = Encoding.UTF8.GetString(bfile, 0, lfile);
			}

			reader.Close();

			return e;
		}
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

