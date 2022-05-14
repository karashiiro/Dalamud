using System;
using System.Diagnostics;

using Dalamud.IoC;
using Dalamud.IoC.Internal;

namespace Dalamud.Game;

/// <summary>
/// A SigScanner facilitates searching for memory signatures in a given ProcessModule.
/// </summary>
[PluginInterface]
[InterfaceVersion("1.0")]
public interface ISigScanner
{
    /// <summary>
    /// Gets a value indicating whether or not the search on this module is performed on a copy.
    /// </summary>
    bool IsCopy { get; }

    /// <summary>
    /// Gets a value indicating whether or not the ProcessModule is 32-bit.
    /// </summary>
    bool Is32BitProcess { get; }

    /// <summary>
    /// Gets the base address of the search area. When copied, this will be the address of the copy.
    /// </summary>
    IntPtr SearchBase { get; }

    /// <summary>
    /// Gets the base address of the .text section search area.
    /// </summary>
    IntPtr TextSectionBase { get; }

    /// <summary>
    /// Gets the offset of the .text section from the base of the module.
    /// </summary>
    long TextSectionOffset { get; }

    /// <summary>
    /// Gets the size of the text section.
    /// </summary>
    int TextSectionSize { get; }

    /// <summary>
    /// Gets the base address of the .data section search area.
    /// </summary>
    IntPtr DataSectionBase { get; }

    /// <summary>
    /// Gets the offset of the .data section from the base of the module.
    /// </summary>
    long DataSectionOffset { get; }

    /// <summary>
    /// Gets the size of the .data section.
    /// </summary>
    int DataSectionSize { get; }

    /// <summary>
    /// Gets the base address of the .rdata section search area.
    /// </summary>
    IntPtr RDataSectionBase { get; }

    /// <summary>
    /// Gets the offset of the .rdata section from the base of the module.
    /// </summary>
    long RDataSectionOffset { get; }

    /// <summary>
    /// Gets the size of the .rdata section.
    /// </summary>
    int RDataSectionSize { get; }

    /// <summary>
    /// Gets the ProcessModule on which the search is performed.
    /// </summary>
    ProcessModule Module { get; }

    /// <summary>
    /// Scan for a .data address using a .text function.
    /// This is intended to be used with IDA sigs.
    /// Place your cursor on the line calling a static address, and create and IDA sig.
    /// </summary>
    /// <param name="signature">The signature of the function using the data.</param>
    /// <param name="offset">The offset from function start of the instruction using the data.</param>
    /// <returns>An IntPtr to the static memory location.</returns>
    IntPtr GetStaticAddressFromSig(string signature, int offset = 0);

    /// <summary>
    /// Try scanning for a .data address using a .text function.
    /// This is intended to be used with IDA sigs.
    /// Place your cursor on the line calling a static address, and create and IDA sig.
    /// </summary>
    /// <param name="signature">The signature of the function using the data.</param>
    /// <param name="result">An IntPtr to the static memory location, if found.</param>
    /// <param name="offset">The offset from function start of the instruction using the data.</param>
    /// <returns>true if the signature was found.</returns>
    bool TryGetStaticAddressFromSig(string signature, out IntPtr result, int offset = 0);

    /// <summary>
    /// Scan for a byte signature in the .data section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <returns>The real offset of the found signature.</returns>
    IntPtr ScanData(string signature);

    /// <summary>
    /// Try scanning for a byte signature in the .data section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <param name="result">The real offset of the signature, if found.</param>
    /// <returns>true if the signature was found.</returns>
    bool TryScanData(string signature, out IntPtr result);

    /// <summary>
    /// Scan for a byte signature in the whole module search area.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <returns>The real offset of the found signature.</returns>
    IntPtr ScanModule(string signature);

    /// <summary>
    /// Try scanning for a byte signature in the whole module search area.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <param name="result">The real offset of the signature, if found.</param>
    /// <returns>true if the signature was found.</returns>
    bool TryScanModule(string signature, out IntPtr result);

    /// <summary>
    /// Resolve a RVA address.
    /// </summary>
    /// <param name="nextInstAddr">The address of the next instruction.</param>
    /// <param name="relOffset">The relative offset.</param>
    /// <returns>The calculated offset.</returns>
    IntPtr ResolveRelativeAddress(IntPtr nextInstAddr, int relOffset);

    /// <summary>
    /// Scan for a byte signature in the .text section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <returns>The real offset of the found signature.</returns>
    IntPtr ScanText(string signature);

    /// <summary>
    /// Try scanning for a byte signature in the .text section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <param name="result">The real offset of the signature, if found.</param>
    /// <returns>true if the signature was found.</returns>
    bool TryScanText(string signature, out IntPtr result);
}
