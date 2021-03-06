using Microsoft.AspNetCore.SignalR.Client;
using Remotely.Agent.Interfaces;
using Remotely.Shared.Models;
using Remotely.Shared.Utilities;
using Remotely.Shared.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Remotely.Agent.Services
{

    public class AppLauncherWin : IAppLauncher
    {
        public AppLauncherWin(ConfigService configService)
        {
            ConnectionInfo = configService.GetConnectionInfo();
        }

        private ConnectionInfo ConnectionInfo { get; }

        public async Task<int> LaunchChatService(string orgName, string requesterID, HubConnection hubConnection)
        {
            try
            {
                var rcBinaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Desktop", EnvironmentHelper.DesktopExecutableFileName);
                if (!File.Exists(rcBinaryPath))
                {
                    await hubConnection.SendAsync("DisplayMessage", "Chat executable not found on target device.", "Executable not found on device.", requesterID);
                }


                // Start Desktop app.
                await hubConnection.SendAsync("DisplayMessage", $"Starting chat service...", "Starting chat service.", requesterID);
                if (WindowsIdentity.GetCurrent().IsSystem)
                {
                    var result = Win32Interop.OpenInteractiveProcess($"{rcBinaryPath} -mode Chat -requester \"{requesterID}\" -organization \"{orgName}\"",
                        targetSessionId: -1,
                        forceConsoleSession: false,
                        desktopName: "default",
                        hiddenWindow: false,
                        out var procInfo);
                    if (!result)
                    {
                        await hubConnection.SendAsync("DisplayMessage", "Chat service failed to start on target device.", "Failed to start chat service.", requesterID);
                    }
                    else
                    {
                        return procInfo.dwProcessId;
                    }
                }
                else
                {
                    return Process.Start(rcBinaryPath, $"-mode Chat -requester \"{requesterID}\" -organization \"{orgName}\"").Id;
                }
            }
            catch (Exception ex)
            {
                Logger.Write(ex);
                await hubConnection.SendAsync("DisplayMessage", "Chat service failed to start on target device.", "Failed to start chat service.", requesterID);
            }
            return -1;
        }

        public async Task LaunchRemoteControl(int targetSessionId, string requesterID, string serviceID, HubConnection hubConnection)
        {
            try
            {
                var rcBinaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Desktop", EnvironmentHelper.DesktopExecutableFileName);
                if (!File.Exists(rcBinaryPath))
                {
                    await hubConnection.SendAsync("DisplayMessage", "Remote control executable not found on target device.", "Executable not found on device.", requesterID);
                    return;
                }


                // Start Desktop app.
                await hubConnection.SendAsync("DisplayMessage", $"Starting remote control...", "Starting remote control.", requesterID);
                if (WindowsIdentity.GetCurrent().IsSystem)
                {
                    var result = Win32Interop.OpenInteractiveProcess(rcBinaryPath + $" -mode Unattended -requester \"{requesterID}\" -serviceid \"{serviceID}\" -deviceid {ConnectionInfo.DeviceID} -host {ConnectionInfo.Host}",
                        targetSessionId: targetSessionId,
                        forceConsoleSession: Shlwapi.IsOS(OsType.OS_ANYSERVER) && targetSessionId == -1,
                        desktopName: "default",
                        hiddenWindow: true,
                        out _);
                    if (!result)
                    {
                        await hubConnection.SendAsync("DisplayMessage", "Remote control failed to start on target device.", "Failed to start remote control.", requesterID);
                    }
                }
                else
                {
                    // SignalR Connection IDs might start with a hyphen.  We surround them
                    // with quotes so the command line will be parsed correctly.
                    Process.Start(rcBinaryPath, $"-mode Unattended -requester \"{requesterID}\" -serviceid \"{serviceID}\" -deviceid {ConnectionInfo.DeviceID} -host {ConnectionInfo.Host}");
                }
            }
            catch (Exception ex)
            {
                Logger.Write(ex);
                await hubConnection.SendAsync("DisplayMessage", "Remote control failed to start on target device.", "Failed to start remote control.", requesterID);
            }
        }
        public async Task RestartScreenCaster(List<string> viewerIDs, string serviceID, string requesterID, HubConnection hubConnection, int targetSessionID = -1)
        {
            try
            {
                var rcBinaryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Desktop", EnvironmentHelper.DesktopExecutableFileName);
                // Start Desktop app.                 
                Logger.Write("Restarting screen caster.");
                if (WindowsIdentity.GetCurrent().IsSystem)
                {
                    // Give a little time for session changing, etc.
                    await Task.Delay(1000);

                    var result = Win32Interop.OpenInteractiveProcess(rcBinaryPath + $" -mode Unattended -requester \"{requesterID}\" -serviceid \"{serviceID}\" -deviceid {ConnectionInfo.DeviceID} -host {ConnectionInfo.Host} -relaunch true -viewers {String.Join(",", viewerIDs)}",
                        targetSessionId: targetSessionID,
                        forceConsoleSession: Shlwapi.IsOS(OsType.OS_ANYSERVER) && targetSessionID == -1,
                        desktopName: "default",
                        hiddenWindow: true,
                        out _);

                    if (!result)
                    {
                        Logger.Write("Failed to relaunch screen caster.");
                        await hubConnection.SendAsync("SendConnectionFailedToViewers", viewerIDs);
                        await hubConnection.SendAsync("DisplayMessage", "Remote control failed to start on target device.", "Failed to start remote control.", requesterID);
                    }
                }
                else
                {
                    // SignalR Connection IDs might start with a hyphen.  We surround them
                    // with quotes so the command line will be parsed correctly.
                    Process.Start(rcBinaryPath, $"-mode Unattended -requester \"{requesterID}\" -serviceid \"{serviceID}\" -deviceid {ConnectionInfo.DeviceID} -host {ConnectionInfo.Host} -relaunch true -viewers {String.Join(",", viewerIDs)}");
                }
            }
            catch (Exception ex)
            {
                await hubConnection.SendAsync("SendConnectionFailedToViewers", viewerIDs);
                Logger.Write(ex);
                throw;
            }
        }
    }
}
