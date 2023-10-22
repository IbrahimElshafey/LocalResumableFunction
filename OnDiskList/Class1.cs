using System;
using System.Collections;
using System.Collections.Concurrent;

namespace OnDiskCollection
{
    public class OnDiskList : ConcurrentDictionary<byte[], byte[]>
    {
        /*
         * # Log File Structure
         * - Int block size in bytes
         * - Int key size in bytes
         * - Byte boolean is fixed size items
         * - Byte boolean has key
         * 
         * # Record Structure
         * - Int object/record size
         * - Int Record Type
         * - Int Operation Type
         *   - New Record
         *   - Delete Record
         *   - Full Update Record
         *   - Partial update record
         *   - User definied operation
         * - Key Bytes
         * - Body Bytes
         * - Checksum field
         * - Padding if needed
         */
    }
}
