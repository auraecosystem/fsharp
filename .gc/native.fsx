open System
open System.Runtime.CompilerServices
open Microsoft.FSharp.NativeInterop

#nowarn "9" // Suppress warning for unmanaged/native pointer usage

module NativeMemoryDemo

    /// Pin an array, perform pointer arithmetic, and mutate memory directly
    let processPinnedArray () =
        let data = [| 10; 20; 30; 40; 50 |]
        
        printfn "Before mutation: %A" data

        // 1. Pin the GC-managed array in memory during this scope
        use ptr = fixed data

        // 2. Read values via pointer offset
        let secondElement = NativePtr.get ptr 1
        printfn "Value at index 1: %d" secondElement // Output: 20

        // 3. Perform pointer arithmetic using NativePtr.add
        let thirdPtr = NativePtr.add ptr 2
        NativePtr.write thirdPtr 99 // Mutate array directly at index 2

        // 4. Iterate over memory using raw pointer increments
        let mutable currentPtr = ptr
        for i in 0 .. data.Length - 1 do
            let val' = NativePtr.read currentPtr
            printfn "Address: 0x%X | Value: %d" (NativePtr.toNativeInt currentPtr) val'
            currentPtr <- NativePtr.add currentPtr 1

        printfn "After mutation: %A" data // data.[2] is now 99

    /// Unsafe memory operations using System.Runtime.CompilerServices.Unsafe
    let processUnsafeCasting () =
        let sourceVal = 0x12345678
        
        // Reinterpret-cast a 32-bit integer directly into a byte array / struct without allocation
        let mutable mutableVal = sourceVal
        let firstByte: byte = Unsafe.As<int, byte>(&mutableVal)

        printfn "Raw Integer: 0x%X" sourceVal
        printfn "First Byte (Endian dependent): 0x%X" firstByte

// --- EXECUTION ---
[<EntryPoint>]
let main _ =
    printfn "=== 1. Pinned Pointer Operations ==="
    NativeMemoryDemo.processPinnedArray ()

    printfn "\n=== 2. Unsafe Reinterpret Casting ==="
    NativeMemoryDemo.processUnsafeCasting ()
    0
