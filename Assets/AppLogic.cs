using B83.Win32;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

public class AppLogic : MonoBehaviour
{
	[SerializeField]
	protected Reporter logview;

	protected TcpClient client;


	// Start is called before the first frame update
	void Start()
	{
		logview.Show();

		// no self-logging
		logview.SuppressOwnLogs();

		string[] args = Environment.GetCommandLineArgs();
		if (args.Length > 1)
		{
			if (File.Exists(args[1]))
			{
				ShowLogFromFile(args[1]);
			}
			else if (args.Length > 2)
			{
				// listen for the game remotely sending log data?

				client = new TcpClient(args[1], int.Parse(args[2]));
				client.ReceiveBufferSize = 65534;

				Task.Run(() => ReceiverLoop(client.GetStream()));

				return; // no file dropping in remote mode
			}


			B83.Win32.UnityDragAndDropHook.InstallHook();
			B83.Win32.UnityDragAndDropHook.OnDroppedFiles += OnFileDrop;
		}


	}

	private async void ReceiverLoop(NetworkStream networkStream)
	{
		try
		{
			while (networkStream != null && networkStream.CanRead)
			{
				await Task.Delay(100);



				byte[] rxBuffer = new byte[65534];
				int nRead = 0;

				// Incoming message may be larger than the buffer size.
				do
				{
					nRead = networkStream.Read(rxBuffer, 0, rxBuffer.Length);
				}
				while (networkStream.DataAvailable);

				DebugLogEntry e = DebugLogEntry.FromByteArray(rxBuffer, 0, nRead);
				if (!e.IsNullOrEmpty())
				{
					LogEntry(e);
				}
				else
				{
					break;
				}
			}
		}
		finally
		{
			networkStream.Close();
			client.Close();

			Application.Quit();
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

				// regexes for things unity adds to the log and annoyingly doesn't annotate in any way

				// shader errors and warnings are thankfully prefixed			

				if (Regex.IsMatch(lines[i], @"ERROR:")
					|| Regex.IsMatch(lines[i], @"^Crash!!!"))
				{
					logview.AddLog(lines[i], string.Empty, LogType.Error);
					entrystart = i + 1;
				}
				else if (Regex.IsMatch(lines[i], @"WARNING:")
					  || Regex.IsMatch(lines[i], @"^Fallback handler")
					  || Regex.IsMatch(lines[i], @"^[Dd]3[Dd]")
					  || Regex.IsMatch(lines[i], @"^[uU]ploading [cC]rash [rR]eport"))
				{
					logview.AddLog(lines[i], string.Empty, LogType.Warning);
					entrystart = i + 1;
				}
				else if (Regex.IsMatch(lines[i], @"^Unloading.+?[Uu]nused")
					  || Regex.IsMatch(lines[i], @"^System Memory")
					  || Regex.IsMatch(lines[i], @"^Total:.+?CreateObjectMapping")
					  || Regex.IsMatch(lines[i], @"^UnloadTime:")
					  || Regex.IsMatch(lines[i], @"^Log:")
					  || Regex.IsMatch(lines[i], @"^Setting up.+?threads for Enlighten")
					  || Regex.IsMatch(lines[i], @"Thread -> id:"))
				{
					logview.AddLog(lines[i], string.Empty, LogType.Log);
					entrystart = i + 1;
				}
				else if (lines[i].StartsWith("(Filename:"))
				{
					DebugLogEntry e = new DebugLogEntry()
					{
						header = lines[entrystart],
						callstack = lines.Skip(entrystart + 1).Take(i - entrystart - 1).Aggregate(string.Empty, (txt, l) =>
						  {
							  txt += l + "\n";
							  return txt;
						  }),
						filename = lines[i]
					}.FigureOutLogType();
					entrystart = i + 1;


					LogEntry(e);
				}
			}
		}
	}

	private void LogEntry(byte[] leData) => LogEntry(DebugLogEntry.FromByteArray(leData, 0, leData.Length));

	private void LogEntry(DebugLogEntry e)
	{
		if (!e.IsNullOrEmpty())
			logview.AddLog(e.header, e.callstack + "\n" + e.filename, e.logType);
	}

	public void OnDestroy()
	{

	}

	public void OnApplicationQuit()
	{
		if (!Application.isEditor)
		{
			// workaround for the crash-on-exit problem https://answers.unity.com/questions/467030/unity-builds-crash-when-i-exit-1.html
			System.Diagnostics.Process.GetCurrentProcess().Kill();
		}
	}

}

