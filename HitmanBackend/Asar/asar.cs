using System.Runtime.InteropServices;

namespace AsarCLR
{
    /// <summary>
    /// Contains various functions to apply patches.
    /// </summary>
    public static unsafe class Asar
    {
        /// <summary>Minimum native API version and expected major version.</summary>
        const int expectedapiversion = 303;
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        /// <summary>Initializes the native library state.</summary>
        [DllImport("asar", EntryPoint = "asar_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool asar_init();

        /// <summary>Releases the native library state.</summary>
        [DllImport("asar", EntryPoint = "asar_close", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool asar_close();

        /// <summary>Queries the assembler version.</summary>
        [DllImport("asar", EntryPoint = "asar_version", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int asar_version();

        /// <summary>Queries the native API version.</summary>
        [DllImport("asar", EntryPoint = "asar_apiversion", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int asar_apiversion();

        /// <summary>Resets the assembler state.</summary>
        [DllImport("asar", EntryPoint = "asar_reset", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool asar_reset();

        /// <summary>Applies a patch to a ROM buffer using the basic signature.</summary>
        [DllImport("asar", EntryPoint = "asar_patch", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool asar_patch(string patchLocation, byte* romData, int bufLen, int* romLength);

        /// <summary>Applies a patch using the extended parameter block.</summary>
        [DllImport("asar", EntryPoint = "asar_patch_ex", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool asar_patch_ex(ref rawPatchParams parameters);

        /// <summary>Queries the largest ROM buffer supported by Asar.</summary>
        [DllImport("asar", EntryPoint = "asar_maxromsize", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int asar_maxromsize();

        /// <summary>Obtains the native error buffer and its length.</summary>
        [DllImport("asar", EntryPoint = "asar_geterrors", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern rawAsarError* asar_geterrors(out int length);

        /// <summary>Obtains the native warning buffer and its length.</summary>
        [DllImport("asar", EntryPoint = "asar_getwarnings", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern rawAsarError* asar_getwarnings(out int length);

        /// <summary>Obtains strings emitted by print directives.</summary>
        [DllImport("asar", EntryPoint = "asar_getprints", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern void** asar_getprints(out int length);

        /// <summary>Enumerates the labels resolved by Asar.</summary>
        [DllImport("asar", EntryPoint = "asar_getalllabels", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern rawAsarLabel* asar_getalllabels(out int length);

        /// <summary>Queries the address of a named label.</summary>
        [DllImport("asar", EntryPoint = "asar_getlabelval", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int asar_getlabelval(string labelName);

        /// <summary>Queries the text of a named define.</summary>
        [DllImport("asar", EntryPoint = "asar_getdefine", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr asar_getdefine(string defineName);

        /// <summary>Enumerates the defines known to Asar.</summary>
        [DllImport("asar", EntryPoint = "asar_getalldefines", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern rawAsarDefine* asar_getalldefines(out int length);

        /// <summary>Expands defines in text using the assembler state.</summary>
        [DllImport("asar", EntryPoint = "asar_resolvedefines", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr asar_resolvedefines(string data, bool learnNew);

        /// <summary>Evaluates an Asar expression and exposes its error if evaluation fails.</summary>
        [DllImport("asar", EntryPoint = "asar_math", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern double asar_math(string math, out IntPtr error);

        /// <summary>Obtains the ranges written to the ROM by the last operation.</summary>
        [DllImport("asar", EntryPoint = "asar_getwrittenblocks", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern rawAsarWrittenBlock* asar_getwrittenblocks(out int length);

        /// <summary>Returns the mapper selected by Asar.</summary>
        [DllImport("asar", EntryPoint = "asar_getmapper", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern MapperType asar_getmapper();

        /// <summary>Generates symbol text in the requested format.</summary>
        [DllImport("asar", EntryPoint = "asar_getsymbolsfile", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr asar_getsymbolsfile(string format);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        /// <summary>
        /// Loads and initializes the DLL. You must call this before using any other Asar function.
        /// </summary>
        /// <returns>True if success</returns>
        public static bool Init()
        {
            try
            {
                if (ApiVersion() < expectedapiversion || (ApiVersion() / 100) > (expectedapiversion / 100))
                {
                    return false;
                }

                if (!asar_init())
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Closes Asar DLL. Call this when you're done using Asar functions.
        /// </summary>
        public static void Close()
            => asar_close();

        /// <summary>
        /// Returns the version, in the format major*10000+minor*100+bugfix*1.
        /// This means that 1.2.34 would be returned as 10234.
        /// </summary>
        /// <returns>Asar version</returns>
        public static int Version()
            => asar_version();

        /// <summary>
        /// Returns the API version, format major*100+minor. Minor is incremented on backwards compatible
        ///  changes; major is incremented on incompatible changes. Does not have any correlation with the
        ///  Asar version.
        /// It's not very useful directly, since Asar.init() verifies this automatically.
        /// </summary>
        /// <returns>Asar API version</returns>
        public static int ApiVersion()
            => asar_apiversion();

        /// <summary>
        /// Clears out all errors, warnings and printed statements, and clears the file cache.
        /// Not useful for much, since patch() already does this.
        /// </summary>
        /// <returns>True if success</returns>
        public static bool Reset()
            => asar_reset();

        /// <summary>Native patch parameter layout passed to Asar through P/Invoke.</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct rawPatchParams
        {
            /// <summary>Size of the parameter layout in bytes.</summary>
            public int StructSize;
            /// <summary>Path of the ASM patch.</summary>
            public string PatchLoc;
            /// <summary>Pointer to the writable ROM buffer.</summary>
            public byte* RomData;
            /// <summary>Total ROM buffer capacity.</summary>
            public int BufLen;
            /// <summary>Pointer to the effective ROM length, which Asar can update.</summary>
            public int* RomLen;
            /// <summary>Pointers to include search paths.</summary>
            public byte** IncludePaths;
            /// <summary>Number of include search paths.</summary>
            public int NumIncludePaths;
            /// <summary>Whether Asar should discard its state before assembling.</summary>
            [MarshalAs(UnmanagedType.I1)]
            public bool ShouldReset;
            /// <summary>Pointer to extra defines passed to the assembler.</summary>
            public rawAsarDefine* AdditionalDefines;
            /// <summary>Number of extra defines.</summary>
            public int AdditionalDefineCount;
            /// <summary>Configuration file for standard include paths.</summary>
            public string STDIncludesFile;
            /// <summary>Configuration file for standard defines.</summary>
            public string STDDefinesFile;
        };

        /// <summary>
        /// Applies a patch.
        /// </summary>
        /// <param name="patchLocation">The patch location.</param>
        /// <param name="romData">The rom data. It must not be headered.</param>
        /// <param name="includePaths">lists additional include paths</param>
        /// <param name="shouldReset">specifies whether asar should clear out all defines, labels,
        /// etc from the last inserted file. Setting it to False will make Asar act like the
        /// currently patched file was directly appended to the previous one.</param>
        /// <param name="additionalDefines">specifies extra defines to give to the patch</param>
        /// <param name="stdIncludeFile">path to a file that specifes additional include paths</param>
        /// <param name="stdDefineFile">path to a file that specifes additional defines</param>
        /// <returns>True if no errors.</returns>
        public static bool Patch(string patchLocation, ref byte[] romData, string[]? includePaths = null,
            bool shouldReset = true, Dictionary<string, string>? additionalDefines = null,
            string? stdIncludeFile = null, string? stdDefineFile = null)
        {
            includePaths ??= Array.Empty<string>();

            additionalDefines ??= new Dictionary<string, string>();

            var includes = new byte*[includePaths.Length];
            var defines = new rawAsarDefine[additionalDefines.Count];

            try
            {
                for (int i = 0; i < includePaths.Length; i++)
                {
                    includes[i] = (byte*)Marshal.StringToCoTaskMemAnsi(includePaths[i]);
                }

                var keys = additionalDefines.Keys.ToArray();

                for (int i = 0; i < additionalDefines.Count; i++)
                {
                    var name = keys[i];
                    var value = additionalDefines[name];
                    defines[i].Name = Marshal.StringToCoTaskMemAnsi(name);
                    defines[i].Contents = Marshal.StringToCoTaskMemAnsi(value);
                }

                int newsize = MaxRomSize();
                int length = romData.Length;

                if (length < newsize)
                {
                    Array.Resize(ref romData, newsize);
                }

                bool success;

                fixed (byte* ptr = romData)
                fixed (byte** includepaths = includes)
                fixed (rawAsarDefine* additional_defines = defines)
                {
                    var param = new rawPatchParams
                    {
                        PatchLoc = patchLocation,
                        RomData = ptr,
                        BufLen = newsize,
                        RomLen = &length,

                        ShouldReset = shouldReset,
                        IncludePaths = includepaths,
                        NumIncludePaths = includes.Length,
                        AdditionalDefines = additional_defines,
                        AdditionalDefineCount = defines.Length,
                        STDDefinesFile = stdDefineFile!,
                        STDIncludesFile = stdIncludeFile!
                    };
                    param.StructSize = Marshal.SizeOf(param);

                    success = asar_patch_ex(ref param);
                }

                if (length < newsize)
                {
                    Array.Resize(ref romData, length);
                }

                return success;
            }
            finally
            {
                for (int i = 0; i < includes.Length; i++)
                {
                    Marshal.FreeCoTaskMem((IntPtr)includes[i]);
                }

                foreach (var define in defines)
                {
                    Marshal.FreeCoTaskMem(define.Name);
                    Marshal.FreeCoTaskMem(define.Contents);
                }
            }
        }

        /// <summary>
        /// Returns the maximum possible size of the output ROM from asar_patch().
        /// Giving this size to buflen guarantees you will not get any buffer too small errors;
        /// however, it is safe to give smaller buffers if you don't expect any ROMs larger
        /// than 4MB or something. It's not very useful directly, since Asar.patch() uses this automatically.
        /// </summary>
        /// <returns>Maximum output size of the ROM.</returns>
        public static int MaxRomSize()
            => asar_maxromsize();

        /// <summary>Native diagnostic layout containing string pointers.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct rawAsarError
        {
            /// <summary>Diagnostic with its location and context.</summary>
            public IntPtr FullErrData;
            /// <summary>Base text of the diagnostic.</summary>
            public IntPtr RawErrData;
            /// <summary>ASM snippet associated with the diagnostic.</summary>
            public IntPtr Block;
            /// <summary>File where the diagnostic originated.</summary>
            public IntPtr Filename;
            /// <summary>Line where the diagnostic originated.</summary>
            public int Line;
            /// <summary>File that invoked the code that produced the diagnostic.</summary>
            public IntPtr CallerFileName;
            /// <summary>Caller line associated with the diagnostic.</summary>
            public int CallerLine;
            /// <summary>Numeric diagnostic identifier.</summary>
            public int ErrID;
        };

        /// <summary>Copies native diagnostics into managed objects while their pointers remain valid.</summary>
        private static Asarerror[] cleanErrors(rawAsarError* ptr, int length)
        {
            Asarerror[] output = new Asarerror[length];

            // Copy unmanaged to managed memory to avoid potential errors in case the area
            // gets cleared by Asar.
            for (int i = 0; i < length; i++)
            {
                output[i].Fullerrdata = Marshal.PtrToStringAnsi(ptr[i].FullErrData)!;
                output[i].Rawerrdata = Marshal.PtrToStringAnsi(ptr[i].RawErrData)!;
                output[i].Block = Marshal.PtrToStringAnsi(ptr[i].Block)!;
                output[i].Filename = Marshal.PtrToStringAnsi(ptr[i].Filename)!;
                output[i].Line = ptr[i].Line;
                output[i].Callerfilename = Marshal.PtrToStringAnsi(ptr[i].CallerFileName)!;
                output[i].Callerline = ptr[i].CallerLine;
                output[i].ErrorId = ptr[i].ErrID;
            }

            return output;
        }

        /// <summary>
        /// Gets all Asar current errors. They're safe to keep for as long as you want.
        /// </summary>
        /// <returns>All Asar's errors.</returns>
        public static Asarerror[] GetErrors()
        {
            rawAsarError* ptr = asar_geterrors(out int length);
            return cleanErrors(ptr, length);
        }

        /// <summary>
        /// Gets all Asar current warning. They're safe to keep for as long as you want.
        /// </summary>
        /// <returns>All Asar's warnings.</returns>
        public static Asarerror[] GetWarnings()
        {
            rawAsarError* ptr = asar_getwarnings(out int length);
            return cleanErrors(ptr, length);
        }

        /// <summary>
        /// Gets all prints generated by the patch
        /// (Note: to see warnings/errors, check getwarnings() and geterrors()
        /// </summary>
        /// <returns>All prints</returns>
        public static string[] GetPrints()
        {
            void** ptr = asar_getprints(out int length);

            string[] output = new string[length];

            for (int i = 0; i < length; i++)
            {
                output[i] = Marshal.PtrToStringAnsi((IntPtr)ptr[i])!;
            }

            return output;
        }

        /// <summary>Native label entry with a name pointer and address.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct rawAsarLabel
        {
            /// <summary>Name of the label or define.</summary>
            public IntPtr Name;
            /// <summary>SNES address of the label.</summary>
            public int Location;
        }

        /// <summary>
        /// Gets all Asar current labels. They're safe to keep for as long as you want.
        /// </summary>
        /// <returns>All Asar's labels.</returns>
        public static Asarlabel[] GetLabels()
        {
            rawAsarLabel* ptr = asar_getalllabels(out int length);
            Asarlabel[] output = new Asarlabel[length];

            // Copy unmanaged to managed memory to avoid potential errors in case the area
            // gets cleared by Asar.
            for (int i = 0; i < length; i++)
            {
                output[i].Name = Marshal.PtrToStringAnsi(ptr[i].Name)!;
                output[i].Location = ptr[i].Location;
            }

            return output;
        }

        /// <summary>
        /// Gets a value of a specific label. Returns "-1" if label has not found.
        /// </summary>
        /// <param name="labelName">The label name.</param>
        /// <returns>The value of label. If not found, it returns -1 here.</returns>
        public static int GetLabelVal(string labelName)
            => asar_getlabelval(labelName);

        /// <summary>
        /// Gets contents of a define. If define doesn't exists, a null string will be generated.
        /// </summary>
        /// <param name="defineName">The define name.</param>
        /// <returns>The define content. If define has not found, this will be null.</returns>
        public static string? GetDefine(string defineName)
            => Marshal.PtrToStringAnsi(asar_getdefine(defineName));

        /// <summary>Native define entry with name and value pointers.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct rawAsarDefine
        {
            /// <summary>Name of the label or define.</summary>
            public IntPtr Name;
            /// <summary>Text associated with the define.</summary>
            public IntPtr Contents;
        }
        /// <summary>
        /// Gets all Asar current defines. They're safe to keep for as long as you want.
        /// </summary>
        /// <returns>All Asar's defines.</returns>
        public static Asardefine[] GetAllDefines()
        {
            rawAsarDefine* ptr = asar_getalldefines(out int length);
            Asardefine[] output = new Asardefine[length];

            // Copy unmanaged to managed memory to avoid potential errors in case the area
            // gets cleared by Asar.
            for (int i = 0; i < length; i++)
            {
                output[i].Name = Marshal.PtrToStringAnsi(ptr[i].Name)!;
                output[i].Contents = Marshal.PtrToStringAnsi(ptr[i].Contents)!;
            }

            return output;
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="learnNew"></param>
        /// <returns></returns>
        public static string? ResolveDefines(string data, bool learnNew)
            => Marshal.PtrToStringAnsi(asar_resolvedefines(data, learnNew));

        /// <summary>
        /// Parse a string of math.
        /// </summary>
        /// <param name="math">The math string, i.e "1+1"</param>
        /// <param name="error">If occurs any error, it will showed here.</param>
        /// <returns>Product.</returns>
        public static double Math(string math, out string? error)
        {
            double value = asar_math(math, out IntPtr err);

            error = Marshal.PtrToStringAnsi(err);
            return value;
        }


        /// <summary>Range of bytes written by Asar in PC and SNES coordinates.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct rawAsarWrittenBlock
        {
            /// <summary>Offset of the block in the ROM file.</summary>
            public int PCOffset;
            /// <summary>SNES address corresponding to the block.</summary>
            public int SNESOffset;
            /// <summary>Length of the modified block in bytes.</summary>
            public int NumBytes;
        };

        /// <summary>Copies native write ranges into a managed array.</summary>
        private static Asarwrittenblock[] cleanWrittenBlocks(rawAsarWrittenBlock* ptr, int length)
        {
            Asarwrittenblock[] output = new Asarwrittenblock[length];

            // Copy unmanaged to managed memory to avoid potential errors in case the area
            // gets cleared by Asar.
            for (int i = 0; i < length; i++)
            {
                output[i].Snesoffset = ptr[i].SNESOffset;
                output[i].Numbytes = ptr[i].NumBytes;
                output[i].Pcoffset = ptr[i].PCOffset;
            }

            return output;
        }

        /// <summary>
        /// Gets all Asar blocks written to the ROM. They're safe to keep for as long as you want.
        /// </summary>
        /// <returns>All Asar's blocks written to the ROM.</returns>
        public static Asarwrittenblock[] GetWrittenBlocks()
        {
            rawAsarWrittenBlock* ptr = asar_getwrittenblocks(out int length);
            return cleanWrittenBlocks(ptr, length);
        }

        /// <summary>
        /// Gets mapper currently used by Asar.
        /// </summary>
        /// <returns>Returns mapper currently used by Asar.</returns>
        public static MapperType GetMapper()
            => asar_getmapper();

        /// <summary>
        /// Generates the contents of a symbols file for in a specific format.
        /// </summary>
        /// <param name="format">The symbol file format to generate</param>
        /// <returns>Returns the textual contents of the symbols file.</returns>
        public static string? GetSymbolsFile(string format = "wla")
            => Marshal.PtrToStringAnsi(asar_getsymbolsfile(format));
    }

    /// <summary>
    /// Contains full information of a Asar error or warning.
    /// </summary>
    public struct Asarerror
    {
        /// <summary>Full diagnostic with its location and context.</summary>
        public string Fullerrdata;
        /// <summary>Base text of the diagnostic.</summary>
        public string Rawerrdata;
        /// <summary>ASM snippet associated with the diagnostic.</summary>
        public string Block;
        /// <summary>File where the diagnostic originated.</summary>
        public string Filename;
        /// <summary>Line where the diagnostic originated.</summary>
        public int Line;
        /// <summary>File that invoked the code that produced the diagnostic.</summary>
        public string Callerfilename;
        /// <summary>Caller line associated with the diagnostic.</summary>
        public int Callerline;
        /// <summary>Numeric diagnostic identifier.</summary>
        public int ErrorId;
    }

    /// <summary>
    /// Contains a label from Asar.
    /// </summary>
    public struct Asarlabel
    {
        /// <summary>Name of the label or define.</summary>
        public string Name;
        /// <summary>SNES address of the label.</summary>
        public int Location;
    }

    /// <summary>
    /// Contains a Asar define.
    /// </summary>
    public struct Asardefine
    {
        /// <summary>Name of the label or define.</summary>
        public string Name;
        /// <summary>Text associated with the define.</summary>
        public string Contents;
    }

    /// <summary>
    /// Contains full information on a block written to the ROM.
    /// </summary>
    public struct Asarwrittenblock
    {
        /// <summary>Offset of the block in the ROM file.</summary>
        public int Pcoffset;
        /// <summary>SNES address corresponding to the block.</summary>
        public int Snesoffset;
        /// <summary>Length of the modified block in bytes.</summary>
        public int Numbytes;
    }

    /// <summary>
    /// Defines the ROM mapper used.
    /// </summary>
    public enum MapperType
    {
        /// <summary>
        /// Invalid map.
        /// </summary>
        InvalidMapper,

        /// <summary>
        /// Standard LoROM.
        /// </summary>
        LoRom,

        /// <summary>
        /// Standard HiROM.
        /// </summary>
        HiRom,

        /// <summary>
        /// SA-1 ROM.
        /// </summary>
        Sa1Rom,

        /// <summary>
        /// SA-1 ROM with 8 MB mapped at once.
        /// </summary>
        BigSa1Rom,

        /// <summary>
        /// Super FX ROM.
        /// </summary>
        SfxRom,

        /// <summary>
        /// ExLoROM.
        /// </summary>
        ExLoRom,

        /// <summary>
        /// ExHiROM.
        /// </summary>
        ExHiRom,

        /// <summary>
        /// No specific ROM mapping.
        /// </summary>
        NoRom
    }
}
