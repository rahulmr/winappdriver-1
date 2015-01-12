﻿namespace WinAppDriver
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using System.Runtime.InteropServices;

    internal interface IStoreApplication : IApplication
    {
        string AppUserModelId { get; }

        string PackageFamilyName { get; }

        string PackageFullName { get; }

        string PackageFolderDir { get; }
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:ElementsMustAppearInTheCorrectOrder", Justification = "Reviewed.")]
    internal class StoreApplication : IStoreApplication
    {
        private string packageNameCache = null;

        private string packageFamilyNameCache = null;

        private string packageFullNameCache = null;

        private string packageFolderDirCache = null;

        private string initialStatesDirCache = null;

        private IUtils utils;

        public StoreApplication(string appUserModelId, IUtils utils)
        {
            this.AppUserModelId = appUserModelId;
            this.utils = utils;
        }

        public string AppUserModelId
        {
            get;
            private set;
        }

        public string PackageFamilyName
        {
            get
            {
                if (this.packageFamilyNameCache == null)
                {
                    // AppUserModelId = {PackageFamilyName}!{AppId}
                    int index = this.AppUserModelId.IndexOf('!');
                    if (index == -1)
                    {
                        string msg = string.Format(
                            "Invalid Application User Model ID: {0}",
                            this.AppUserModelId);
                        throw new WinAppDriverException(msg);
                    }

                    this.packageFamilyNameCache = this.AppUserModelId.Substring(0, index);
                }

                return this.packageFamilyNameCache;
            }
        }

        public string PackageFullName
        {
            get
            {
                if (this.packageFullNameCache == null)
                {
                    if (this.IsInstalled())
                    {
                        // PackageFamilyName = {Name}_{PublisherHash}!{AppId}
                        PowerShell ps = PowerShell.Create();
                        ps.AddCommand("Get-AppxPackage");
                        ps.AddParameter("Name", this.PackageName);
                        System.Collections.ObjectModel.Collection<PSObject> package = ps.Invoke();

                        this.packageFullNameCache = package[0].Members["PackageFullName"].Value.ToString();
                    }
                    else
                    {
                        string msg = string.Format("Application is not installed, cannot find PackageFullName.");
                        throw new WinAppDriverException(msg);
                    }
                }

                return this.packageFullNameCache;
            }
        }

        public string PackageFolderDir
        {
            get
            {
                if (this.packageFolderDirCache == null)
                {
                    this.packageFolderDirCache = this.utils.ExpandEnvironmentVariables(
                        @"%LOCALAPPDATA%\Packages\" + this.PackageFamilyName);
                }

                return this.packageFolderDirCache;
            }
        }

        public bool IsInstalled()
        {
            PowerShell ps = PowerShell.Create();
            ps.AddCommand("Get-AppxPackage");
            ps.AddParameter("Name", this.PackageName);
            System.Collections.ObjectModel.Collection<PSObject> package = ps.Invoke();
            if (package.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Activate()
        {
            // TODO thorw exception if needed
            Process.Start("ActivateStoreApp", this.AppUserModelId);
        }

        public void Terminate()
        {
            var api = (IPackageDebugSettings)new PackageDebugSettings();
            api.TerminateAllProcesses(this.PackageFullName);
        }

        public void BackupInitialStates()
        {
            this.utils.CopyDirectory(this.PackageFolderDir + @"\Settings", this.InitialStatesDir + @"\Settings");
            this.utils.CopyDirectory(this.PackageFolderDir + @"\LocalState", this.InitialStatesDir + @"\LocalState");
        }

        public void RestoreInitialStates()
        {
            this.utils.CopyDirectory(this.InitialStatesDir + @"\Settings", this.PackageFolderDir + @"\Settings");
            this.utils.CopyDirectory(this.InitialStatesDir + @"\LocalState", this.PackageFolderDir + @"\LocalState");
        }

        private string PackageName
        {
            get
            {
                if (this.packageNameCache == null)
                {
                    // PackageFamilyName = {Name}_{PublisherHash}!{AppId}
                    this.packageNameCache = this.PackageFamilyName.Remove(this.PackageFamilyName.IndexOf("_"));
                }

                return this.packageNameCache;
            }
        }

        private string InitialStatesDir
        {
            get
            {
                if (this.initialStatesDirCache == null)
                {
                    this.initialStatesDirCache = this.utils.ExpandEnvironmentVariables(
                        @"%LOCALAPPDATA%\WinAppDriver\Packages\" + this.PackageFamilyName);
                }

                return this.initialStatesDirCache;
            }
        }

        [ComImport, Guid("B1AEC16F-2383-4852-B0E9-8F0B1DC66B4D")]
        private class PackageDebugSettings
        {
        }

        private enum PACKAGE_EXECUTION_STATE
        {
            PES_UNKNOWN,
            PES_RUNNING,
            PES_SUSPENDING,
            PES_SUSPENDED,
            PES_TERMINATED
        }

        [ComImport, Guid("F27C3930-8029-4AD1-94E3-3DBA417810C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed.")]
        private interface IPackageDebugSettings
        {
            int EnableDebugging(
                [MarshalAs(UnmanagedType.LPWStr)] string packageFullName,
                [MarshalAs(UnmanagedType.LPWStr)] string debuggerCommandLine,
                IntPtr environment);

            int DisableDebugging([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

            int Suspend([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

            int Resume([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

            int TerminateAllProcesses([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

            int SetTargetSessionId(int sessionId);

            int EnumerageBackgroundTasks(
                [MarshalAs(UnmanagedType.LPWStr)] string packageFullName,
                out uint taskCount,
                out int intPtr,
                [Out] string[] array);

            int ActivateBackgroundTask(IntPtr something);

            int StartServicing([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

            int StopServicing([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

            int StartSessionRedirection(
                [MarshalAs(UnmanagedType.LPWStr)] string packageFullName,
                uint sessionId);

            int StopSessionRedirection([MarshalAs(UnmanagedType.LPWStr)] string packageFullName);

            int GetPackageExecutionState(
                [MarshalAs(UnmanagedType.LPWStr)] string packageFullName,
                out PACKAGE_EXECUTION_STATE packageExecutionState);

            int RegisterForPackageStateChanges(
                [MarshalAs(UnmanagedType.LPWStr)] string packageFullName,
                IntPtr pPackageExecutionStateChangeNotification,
                out uint pdwCookie);

            int UnregisterForPackageStateChanges(uint dwCookie);
        }
    }
}