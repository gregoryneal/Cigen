﻿using System;
using System.Collections.Generic;
using OpenCvSharp.Util;

namespace OpenCvSharp
{
    /// <summary>
    /// 
    /// </summary>
    public class VectorOfPoint2f : DisposableCvObject, IStdVector<Point2f>
    {
        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed = false;

        #region Init and Dispose

        /// <summary>
        /// 
        /// </summary>
        public VectorOfPoint2f()
        {
            ptr = NativeMethods.vector_Point2f_new1();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ptr"></param>
        public VectorOfPoint2f(IntPtr ptr)
        {
            this.ptr = ptr;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size"></param>
        public VectorOfPoint2f(int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException("nameof(size)");
            ptr = NativeMethods.vector_Point2f_new2(new IntPtr(size));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public VectorOfPoint2f(IEnumerable<Point2f> data)
        {
            if (data == null)
                throw new ArgumentNullException("nameof(data)");
            Point2f[] array = EnumerableEx.ToArray(data);
            ptr = NativeMethods.vector_Point2f_new3(array, new IntPtr(array.Length));
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals true, the method has been called directly or indirectly by a user's code. Managed and unmanaged resources can be disposed.
        /// If false, the method has been called by the runtime from inside the finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                try
                {
                    if (IsEnabledDispose)
                    {
                        NativeMethods.vector_Point2f_delete(ptr);
                    }
                    disposed = true;
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// vector.size()
        /// </summary>
        public int Size
        {
            get { return NativeMethods.vector_Point2f_getSize(ptr).ToInt32(); }
        }

        /// <summary>
        /// &amp;vector[0]
        /// </summary>
        public IntPtr ElemPtr
        {
            get { return NativeMethods.vector_Point2f_getPointer(ptr); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts std::vector to managed array
        /// </summary>
        /// <returns></returns>
        public Point2f[] ToArray()
        {
            int size = Size;
            if (size == 0)
            {
                return new Point2f[0];
            }
            Point2f[] dst = new Point2f[size];
            using (ArrayAddress1<Point2f> dstPtr = new ArrayAddress1<Point2f>(dst))
            {
                Util.Utility.CopyMemory(dstPtr, ElemPtr, Point2f.SizeOf*dst.Length);
            }
            return dst;
        }

        #endregion
    }
}
