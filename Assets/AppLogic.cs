using B83.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class AppLogic : MonoBehaviour
{
	[SerializeField]
	protected Reporter logview;


	// Start is called before the first frame update
	void Start()
	{
		logview.Show();

		B83.Win32.UnityDragAndDropHook.InstallHook();
		B83.Win32.UnityDragAndDropHook.OnDroppedFiles += OnFileDrop;

		string[] args = Environment.GetCommandLineArgs();
		if (args.Length > 1)
		{
			if (File.Exists(args[1]))
			{
				ShowLogFromFile(args[1]);
			}
		}

	}

	private void OnFileDrop(List<string> aPathNames, POINT aDropPoint)
	{
		if (aPathNames.Count > 0 && File.Exists(aPathNames[0]))
			ShowLogFromFile(aPathNames[0]);
	}

	private void ShowLogFromFile(string filename)
	{
		logview.clear();


		string[] lines = File.ReadAllLines(filename).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();


		int entrystart = -1;
		for (int i = 0; i < lines.Length; i++)
		{
			if (entrystart == -1)
			{
				// parse initial section
				logview.AddLog(lines[i], string.Empty, LogType.Log);

				if (lines[i].StartsWith("UnloadTime:"))
				{
					entrystart = i + 1;
				}
			}
			else
			{
				if (i < entrystart) continue;


				// parse log entries 

				// shader errors and warnings are thankfully prefixed			
				if (Regex.IsMatch(lines[i], @"ERROR:"))
				{
					logview.AddLog(lines[i], string.Empty, LogType.Error);
					entrystart = i + 1;
				}
				else if (Regex.IsMatch(lines[i], @"WARNING:"))
				{
					logview.AddLog(lines[i], string.Empty, LogType.Warning);
					entrystart = i + 1;
				}
				// regexes for things unity adds to the log and annoyingly doesn't annotate in any way
				else if (Regex.IsMatch(lines[i], @"^Unloading.+?[Uu]nused")
					  || Regex.IsMatch(lines[i], @"^System Memory")
					  || Regex.IsMatch(lines[i], @"^Total:.+?CreateObjectMapping")
					  || Regex.IsMatch(lines[i], @"^UnloadTime:")
					  || Regex.IsMatch(lines[i], @"^[uU]ploading [cC]rash [rR]eport")
					  || Regex.IsMatch(lines[i], @"^Setting up.+?threads for Enlighten")
					  || Regex.IsMatch(lines[i], @"Thread -> id:"))
				{
					logview.AddLog(lines[i], string.Empty, LogType.Log);
					entrystart = i + 1;
				}
				else if (Regex.IsMatch(lines[i], @"^Fallback handler"))
				{
					logview.AddLog(lines[i], string.Empty, LogType.Warning);
					entrystart = i + 1;
				}
				else if (lines[i].StartsWith("(Filename:"))
				{
					Entry e = new Entry()
					{
						header = lines[entrystart],
						callstack = lines.Skip(entrystart + 1).Take(i - entrystart - 1).Aggregate(string.Empty, (txt, l) =>
						  {
							  txt += l + "\n";
							  return txt;
						  }),
						filename = lines[i]
					};
					entrystart = i + 1;

					LogEntry(e);
				}
			}
		}
	}

	private void LogEntry(Entry e)
	{
		LogType logType = LogType.Log;


		if (Regex.IsMatch(e.header, @"^\S+?[eE]xception"))
		{
			logType = LogType.Exception;
		}
		else if (Regex.IsMatch(e.header, @"^The referenced script.+?is missing!"))
		{
			logType = LogType.Warning;
		}
		else if (!string.IsNullOrWhiteSpace(e.callstack))
		{
			if (e.callstack.Contains("UnityEngine.Debug:LogError("))
			{
				logType = LogType.Error;
			}
			else if (e.callstack.Contains("UnityEngine.Debug:LogWarning("))
			{
				logType = LogType.Warning;
			}
			else if (e.callstack.Contains("UnityEngine.Debug:LogAssertion("))
			{
				logType = LogType.Assert;
			}
		}

		logview.AddLog(e.header, e.callstack + "\n" + e.filename, logType);
	}
}
public class Entry
{
	public string header;
	public string callstack;
	public string filename;
}

