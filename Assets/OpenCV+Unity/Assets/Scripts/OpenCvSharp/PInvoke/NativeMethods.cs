﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using OpenCvSharp.Util;

// ReSharper disable InconsistentNaming
#pragma warning disable 1591

namespace OpenCvSharp
{
    /// <summary>
    /// P/Invoke methods of OpenCV 2.x C++ interface
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static partial class NativeMethods
    {
        /// <summary>
        /// Is tried P/Invoke once
        /// </summary>
        private static bool tried = false;

        public const string DllVCRuntime = "vcruntime140";
        public const string DllMsvcp = "msvcp140";
//UFIX
#if !UNITY_EDITOR && UNITY_IOS 
		public const string DllExtern = "__Internal";
#else
		public const string DllExtern = "OpenCvSharpExtern";
#endif
        public const string Version = "320";

        private static readonly string[] RuntimeDllNames =
        {
            //DllVCRuntime,
            //DllMsvcp,
        };

        private static readonly string[] OpenCVDllNames =
        {
            "opencv_world",
        };

        public const string DllFfmpegX86 = "opencv_ffmpeg" + Version;
        public const string DllFfmpegX64 = DllFfmpegX86 + "_64";
        
        /// <summary>
        /// Static constructor
        /// </summary>
		#pragma warning disable 0618
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        static NativeMethods()
        {
            LoadLibraries(WindowsLibraryLoader.Instance.AdditionalPaths);

            // call cv to enable redirecting 
            TryPInvoke();
        }
		#pragma warning restore 0618
        /// <summary>
        /// Load DLL files dynamically using Win32 LoadLibrary
        /// </summary>
        /// <param name="additionalPaths"></param>
        public static void LoadLibraries(IEnumerable<string> additionalPaths = null)
        {
            if (IsUnix())
                return;

            string[] ap = EnumerableEx.ToArray(additionalPaths);
            List<string> runtimePaths = new List<string> (ap);
            runtimePaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));
            
            foreach (string dll in RuntimeDllNames)
            {
                WindowsLibraryLoader.Instance.LoadLibrary(dll, runtimePaths);
            }
            foreach (string dll in OpenCVDllNames)
            {
                WindowsLibraryLoader.Instance.LoadLibrary(dll + Version, ap);
            }

            // calib3d, contrib, core, features2d, flann, highgui, imgproc, legacy,
            // ml, nonfree, objdetect, photo, superres, video, videostab
            WindowsLibraryLoader.Instance.LoadLibrary(DllExtern, ap);

            // Redirection of error occurred in native library 
            IntPtr zero = IntPtr.Zero;
            IntPtr current = redirectError(ErrorHandlerThrowException, zero, ref zero);
            if (current != IntPtr.Zero)
            {
                ErrorHandlerDefault = (CvErrorCallback)Marshal.GetDelegateForFunctionPointer(
                    current,
                    typeof(CvErrorCallback)
                );
            }
            else
            {
                ErrorHandlerDefault = null;
            }
        }

        /// <summary>
        /// Checks whether PInvoke functions can be called
        /// </summary>
        private static void TryPInvoke()
        {
            if (tried)
                return;
            tried = true;

            try
            {
                core_Mat_sizeof();
            }
            catch (DllNotFoundException e)
            {
                var exception = PInvokeHelper.CreateException(e);
                try{Console.WriteLine(exception.Message);}
                catch{}
                try{Debug.WriteLine(exception.Message);}
                catch{}
                throw exception;
            }
            catch (BadImageFormatException e)
            {
                var exception = PInvokeHelper.CreateException(e);
                try { Console.WriteLine(exception.Message); }
                catch { }
                try { Debug.WriteLine(exception.Message); }
                catch { }
                throw exception;
            }
            catch (Exception e)
            {
                Exception ex = e.InnerException ?? e;
                try{ Console.WriteLine(ex.Message); }
                catch{}
                try{ Debug.WriteLine(ex.Message); }
                catch{}
                throw;
            }
        }

        /// <summary>
        /// Returns whether the OS is Windows or not
        /// </summary>
        /// <returns></returns>
        public static bool IsWindows()
        {
            return !IsUnix();
        }

        /// <summary>
        /// Returns whether the OS is *nix or not
        /// </summary>
        /// <returns></returns>
        public static bool IsUnix()
        {
            var p = Environment.OSVersion.Platform;
            return (p == PlatformID.Unix ||
                    p == PlatformID.MacOSX ||
                    (int)p == 128);
        }

        /// <summary>
        /// Returns whether the runtime is Mono or not
        /// </summary>
        /// <returns></returns>
        public static bool IsMono()
        {
            return (Type.GetType("Mono.Runtime") != null);
        }

        #region Error redirection
        /// <summary>
        /// Custom error handler to be thrown by OpenCV
        /// </summary>
        public static readonly CvErrorCallback ErrorHandlerThrowException =
            delegate(ErrorCode status, string funcName, string errMsg, string fileName, int line, IntPtr userdata)
            {
                try
                {
                    //cvSetErrStatus(CvStatus.StsOk);
                    return 0;
                }
                finally
                {
                    throw new OpenCVException(status, funcName, errMsg, fileName, line);
                }
            };

        /// <summary>
        /// Custom error handler to ignore all OpenCV errors
        /// </summary>
        public static readonly CvErrorCallback ErrorHandlerIgnorance =
            delegate(ErrorCode status, string funcName, string errMsg, string fileName, int line, IntPtr userdata)
            {
                //cvSetErrStatus(CvStatus.StsOk);
                return 0;
            };

        /// <summary>
        /// Default error handler
        /// </summary>
        public static CvErrorCallback ErrorHandlerDefault;
        #endregion
    }
}