// This file is part of Abanu, an Operating System written in C#. Web: https://www.abanu.org
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

/*
 * This file implements a buddy memory allocator, which is an allocator that
 * allocates memory within a fixed linear address range. It spans the address
 * range with a binary tree that tracks free space. Both "malloc" and "free"
 * are O(log N) time where N is the maximum possible number of allocations.
 *
 * The "buddy" term comes from how the tree is used. When memory is allocated,
 * nodes in the tree are split recursively until a node of the appropriate size
 * is reached. Every split results in two child nodes, each of which is the
 * buddy of the other. When a node is freed, the node and its buddy can be
 * merged again if the buddy is also free. This makes the memory available
 * for larger allocations again.
 */

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter

#if BITS_32
using PointerType = System.UInt32;
#else
using PointerType = System.UInt64;
#endif

namespace Abanu.Kernel.Core
{

    public unsafe abstract class BinaryBuddyAllocator_TestImplementation
    {

        /*
             * Every allocation needs an 8-byte header to store the allocation size while
             * staying 8-byte aligned. The address returned by "malloc" is the address
             * right after this header (i.e. the size occupies the 8 bytes before the
             * returned address).
             */
        private const byte HEADER_SIZE = 8;

        /*
         * The minimum allocation size is 16 bytes because we have an 8-byte header and
         * we need to stay 8-byte aligned.
         */
        private const byte MIN_ALLOC_LOG2 = 4;
        //#define MIN_ALLOC ((size_t)1 << MIN_ALLOC_LOG2)
        private const byte MIN_ALLOC = 1 << MIN_ALLOC_LOG2;

        /*
     * The maximum allocation size is currently set to 2gb. This is the total size
     * of the heap. It's technically also the maximum allocation size because the
     * heap could consist of a single allocation of this size. But of course real
     * heaps will have multiple allocations, so the real maximum allocation limit
     * is at most 1gb.
     */
        private const byte MAX_ALLOC_LOG2 = 31;
        private const uint MAX_ALLOC = unchecked((uint)(1 << MAX_ALLOC_LOG2));

        /*
     * Allocations are done in powers of two starting from MIN_ALLOC and ending at
     * MAX_ALLOC inclusive. Each allocation size has a bucket that stores the free
     * list for that allocation size.
     *
     * Given a bucket index, the size of the allocations in that bucket can be
     * found with "(size_t)1 << (MAX_ALLOC_LOG2 - bucket)".
     */
        protected const byte BUCKET_COUNT = MAX_ALLOC_LOG2 - MIN_ALLOC_LOG2 + 1;

        /*
     * Free lists are stored as circular doubly-linked lists. Every possible
     * allocation size has an associated free list that is threaded through all
     * currently free blocks of that size. That means MIN_ALLOC must be at least
     * "sizeof(list_t)". MIN_ALLOC is currently 16 bytes, so this will be true for
     * both 32-bit and 64-bit.
     */
        protected unsafe struct List_t
        {
            public List_t* prev;
            public List_t* next;
        }

        /*
     * Each bucket corresponds to a certain allocation size and stores a free list
     * for that size. The bucket at index 0 corresponds to an allocation size of
     * MAX_ALLOC (i.e. the whole address space).
     */

        protected List_t* buckets;

        /*
     * We could initialize the allocator by giving it one free block the size of
     * the entire address space. However, this would cause us to instantly reserve
     * half of the entire address space on the first allocation, since the first
     * split would store a free list entry at the start of the right child of the
     * root. Instead, we have the tree start out small and grow the size of the
     * tree as we use more memory. The size of the tree is tracked by this value.
     */
        private byte bucket_limit;

        /*
     * This array represents a linearized binary tree of bits. Every possible
     * allocation larger than MIN_ALLOC has a node in this tree (and therefore a
     * bit in this array).
     *
     * Given the index for a node, linearized binary trees allow you to traverse to
     * the parent node or the child nodes just by doing simple arithmetic on the
     * index:
     *
     * - Move to parent:         index = (index - 1) / 2;
     * - Move to left child:     index = index * 2 + 1;
     * - Move to right child:    index = index * 2 + 2;
     * - Move to sibling:        index = ((index - 1) ^ 1) + 1;
     *
     * Each node in this tree can be in one of several states:
     *
     * - UNUSED (both children are UNUSED)
     * - SPLIT (one child is UNUSED and the other child isn't)
     * - USED (neither children are UNUSED)
     *
     * These states take two bits to store. However, it turns out we have enough
     * information to distinguish between UNUSED and USED from context, so we only
     * need to store SPLIT or not, which only takes a single bit.
     *
     * Note that we don't need to store any nodes for allocations of size MIN_ALLOC
     * since we only ever care about parent nodes.
     */
        //static uint8_t node_is_split[(1 << (BUCKET_COUNT - 1)) / 8];
        //protected byte* node_is_split;

        /*
         * This is the starting address of the address range for this allocator. Every
         * returned allocation will be an offset of this pointer from 0 to MAX_ALLOC.
         */
        //void* base_ptr;

        /*
         * This is the maximum address that has ever been used by the allocator. It's
         * used to know when to call "brk" to request more memory from the kernel.
         */
        //void* max_ptr;

        /*
         * Make sure all addresses before "new_value" are valid and can be used. Memory
         * is allocated in a 2gb address range but that memory is not reserved up
         * front. It's only reserved when it's needed by calling this function. This
         * will return false if the memory could not be reserved.
         */
        //bool update_max_ptr(void* new_value)
        //{
        //    if (new_value > max_ptr)
        //    {
        //        if (brk(new_value))
        //        {
        //            return true;
        //        }
        //        max_ptr = new_value;
        //    }
        //    return false;
        //}

        /*
         * Initialize a list to empty. Because these are circular lists, an "empty"
         * list is an entry where both links point to itself. This makes insertion
         * and removal simpler because they don't need any branches.
         */
        private static void List_init(List_t* list)
        {
            list->prev = list;
            list->next = list;
        }

        /*
         * Append the provided entry to the end of the list. This assumes the entry
         * isn't in a list already because it overwrites the linked list pointers.
         */
        private static void List_push(List_t* list, List_t* entry)
        {
            List_t* prev = list->prev;
            entry->prev = prev;
            entry->next = list;
            prev->next = entry;
            list->prev = entry;
        }

        /*
         * Remove the provided entry from whichever list it's currently in. This
         * assumes that the entry is in a list. You don't need to provide the list
         * because the lists are circular, so the list's pointers will automatically
         * be updated if the first or last entries are removed.
         */
        private static void List_remove(List_t* entry)
        {
            List_t* prev = entry->prev;
            List_t* next = entry->next;
            prev->next = next;
            next->prev = prev;
        }

        /*
         * Remove and return the first entry in the list or NULL if the list is empty.
         */
        private static List_t* List_pop(List_t* list)
        {
            List_t* back = list->prev;
            if (back == list)
                return null;
            List_remove(back);
            return back;
        }

        /*
         * This maps from the index of a node to the address of memory that node
         * represents. The bucket can be derived from the index using a loop but is
         * required to be provided here since having them means we can avoid the loop
         * and have this function return in constant time.
         */
        private Page* Ptr_for_node(uint index, byte bucket)
        {
            //return (void*)(((PointerType)base_ptr) + ((index - (PointerType)(1 << bucket) + 1) << (MAX_ALLOC_LOG2 - bucket)));
            return &firstPage[index];
        }

        public Page* firstPage;

        /*
         * This maps from an address of memory to the node that represents that
         * address. There are often many nodes that all map to the same address, so
         * the bucket is needed to uniquely identify a node.
         */
        private uint Node_for_ptr(Page* ptr, byte bucket)
        {
            //return (uint)((((uint)((PointerType)ptr - (PointerType)base_ptr)) >> (MAX_ALLOC_LOG2 - bucket)) + (1 << bucket) - 1);
            return (uint)((PointerType)((PointerType)ptr - (PointerType)firstPage) / (PointerType)sizeof(Page));
        }

        /*
         * Given the index of a node, this returns the "is split" flag of the parent.
         */
        protected abstract bool Parent_is_split(uint index);

        /*
         * Given the index of a node, this flips the "is split" flag of the parent.
         */
        protected abstract void Flip_parent_is_split(uint index);

        /*
         * Given the requested size passed to "malloc", this function returns the index
         * of the smallest bucket that can fit that size.
         */
        public static byte Bucket_for_request(uint request)
        {
            byte bucket = BUCKET_COUNT - 1;
            uint size = MIN_ALLOC;

            while (size < request)
            {
                bucket--;
                size *= 2;
            }

            return bucket;
        }

        /*
     * The tree is always rooted at the current bucket limit. This call grows the
     * tree by repeatedly doubling it in size until the root lies at the provided
     * bucket index. Each doubling lowers the bucket limit by 1.
     */
        private bool Lower_bucket_limit(byte bucket)
        {
            while (bucket < bucket_limit)
            {
                uint root = Node_for_ptr(firstPage, bucket_limit);
                void* right_child;

                /*
                 * If the parent isn't SPLIT, that means the node at the current bucket
                 * limit is UNUSED and our address space is entirely free. In that case,
                 * clear the root free list, increase the bucket limit, and add a single
                 * block with the newly-expanded address space to the new root free list.
                 */
                if (!Parent_is_split(root))
                {
                    List_remove((List_t*)firstPage);
                    List_init(&buckets[--bucket_limit]);
                    List_push(&buckets[bucket_limit], (List_t*)firstPage);
                    continue;
                }

                /*
                 * Otherwise, the tree is currently in use. Create a parent node for the
                 * current root node in the SPLIT state with a right child on the free
                 * list. Make sure to reserve the memory for the free list entry before
                 * writing to it. Note that we do not need to flip the "is split" flag for
                 * our current parent because it's already on (we know because we just
                 * checked it above).
                 */
                right_child = Ptr_for_node(root + 1, bucket_limit);
                //if (!update_max_ptr((void*)((PointerType)right_child + (PointerType)sizeof(list_t))))
                //{
                //    return false;
                //}
                List_push(&buckets[bucket_limit], (List_t*)right_child);
                List_init(&buckets[--bucket_limit]);

                /*
                 * Set the grandparent's SPLIT flag so if we need to lower the bucket limit
                 * again, we'll know that the new root node we just added is in use.
                 */
                root = (root - 1) / 2;
                if (root != 0)
                {
                    Flip_parent_is_split(root);
                }
            }

            return true;
        }

        public void Init()
        {
            //base_ptr = max_ptr = sbrk(0);
            bucket_limit = BUCKET_COUNT - 1;
            //update_max_ptr((void*)((PointerType)base_ptr + (PointerType)sizeof(list_t)));
            List_init(&buckets[BUCKET_COUNT - 1]);
            List_push(&buckets[BUCKET_COUNT - 1], (List_t*)firstPage);
        }

        public Page* Malloc(byte bucket)
        {
            byte original_bucket;

            /*
             * Make sure it's possible for an allocation of this size to succeed. There's
             * a hard-coded limit on the maximum allocation size because of the way this
             * allocator works.
             */
            if (bucket > MAX_ALLOC_LOG2)
            {
                return null;
            }

            /*
             * Initialize our global state if this is the first call to "malloc". At the
             * beginning, the tree has a single node that represents the smallest
             * possible allocation size. More memory will be reserved later as needed.
             */
            //if (base_ptr == null)
            //{
            //    base_ptr = max_ptr = sbrk(0);
            //    bucket_limit = BUCKET_COUNT - 1;
            //    update_max_ptr((void*)((PointerType)base_ptr + (PointerType)sizeof(list_t)));
            //    list_init(&buckets[BUCKET_COUNT - 1]);
            //    list_push(&buckets[BUCKET_COUNT - 1], (list_t*)base_ptr);
            //}

            /*
             * Find the smallest bucket that will fit this request. This doesn't check
             * that there's space for the request yet.
             */
            //bucket = bucket_for_request(request + HEADER_SIZE);
            original_bucket = bucket;

            /*
             * Search for a bucket with a non-empty free list that's as large or larger
             * than what we need. If there isn't an exact match, we'll need to split a
             * larger one to get a match.
             */
            while (bucket + 1 != 0)
            {
                uint size, bytes_needed, i;
                Page* ptr;

                /*
                 * We may need to grow the tree to be able to fit an allocation of this
                 * size. Try to grow the tree and stop here if we can't.
                 */
                if (!Lower_bucket_limit(bucket))
                {
                    return null;
                }

                /*
                 * Try to pop a block off the free list for this bucket. If the free list
                 * is empty, we're going to have to split a larger block instead.
                 */
                ptr = (Page*)List_pop(&buckets[bucket]);
                if (ptr == null)
                {
                    /*
                     * If we're not at the root of the tree or it's impossible to grow the
                     * tree any more, continue on to the next bucket.
                     */
                    if (bucket != bucket_limit || bucket == 0)
                    {
                        bucket--;
                        continue;
                    }

                    /*
                     * Otherwise, grow the tree one more level and then pop a block off the
                     * free list again. Since we know the root of the tree is used (because
                     * the free list was empty), this will add a parent above this node in
                     * the SPLIT state and then add the new right child node to the free list
                     * for this bucket. Popping the free list will give us this right child.
                     */
                    if (!Lower_bucket_limit((byte)(bucket - 1)))
                    {
                        return null;
                    }
                    ptr = (Page*)List_pop(&buckets[bucket]);
                }

                /*
                 * Try to expand the address space first before going any further. If we
                 * have run out of space, put this block back on the free list and fail.
                 */
                //size = (uint)(1 << (MAX_ALLOC_LOG2 - bucket));
                //bytes_needed = bucket < original_bucket ? size / 2 + (byte)sizeof(list_t) : size;
                //if (!update_max_ptr((void*)((PointerType)ptr + bytes_needed)))
                //{
                //    list_push(&buckets[bucket], (list_t*)ptr);
                //    return null;
                //}

                /*
                 * If we got a node off the free list, change the node from UNUSED to USED.
                 * This involves flipping our parent's "is split" bit because that bit is
                 * the exclusive-or of the UNUSED flags of both children, and our UNUSED
                 * flag (which isn't ever stored explicitly) has just changed.
                 *
                 * Note that we shouldn't ever need to flip the "is split" bit of our
                 * grandparent because we know our buddy is USED so it's impossible for our
                 * grandparent to be UNUSED (if our buddy chunk was UNUSED, our parent
                 * wouldn't ever have been split in the first place).
                 */
                i = Node_for_ptr(ptr, bucket);
                if (i != 0)
                {
                    Flip_parent_is_split(i);
                }

                /*
                 * If the node we got is larger than we need, split it down to the correct
                 * size and put the new unused child nodes on the free list in the
                 * corresponding bucket. This is done by repeatedly moving to the left
                 * child, splitting the parent, and then adding the right child to the free
                 * list.
                 */
                while (bucket < original_bucket)
                {
                    i = (i * 2) + 1;
                    bucket++;
                    Flip_parent_is_split(i);
                    List_push(&buckets[bucket], (List_t*)Ptr_for_node(i + 1, bucket));
                }

                /*
                 * Now that we have a memory address, write the block header (just the size
                 * of the allocation) and return the address immediately after the header.
                 */
                *(uint*)ptr = bucket;
                ptr->bucket = bucket;
                //return (void*)((PointerType)ptr + HEADER_SIZE);
                return ptr;
            }

            return null;
        }

        public void Free(Page* ptr)
        {
            byte bucket;
            uint i;

            /*
             * Ignore any attempts to free a NULL pointer.
             */
            if ((PointerType)ptr == 0)
            {
                return;
            }

            /*
             * We were given the address returned by "malloc" so get back to the actual
             * address of the node by subtracting off the size of the block header. Then
             * look up the index of the node corresponding to this address.
             */
            bucket = ptr->bucket;
            i = Node_for_ptr(ptr, bucket);

            /*
             * Traverse up to the root node, flipping USED blocks to UNUSED and merging
             * UNUSED buddies together into a single UNUSED parent.
             */
            while (i != 0)
            {
                /*
                 * Change this node from UNUSED to USED. This involves flipping our
                 * parent's "is split" bit because that bit is the exclusive-or of the
                 * UNUSED flags of both children, and our UNUSED flag (which isn't ever
                 * stored explicitly) has just changed.
                 */
                Flip_parent_is_split(i);

                /*
                 * If the parent is now SPLIT, that means our buddy is USED, so don't merge
                 * with it. Instead, stop the iteration here and add ourselves to the free
                 * list for our bucket.
                 *
                 * Also stop here if we're at the current root node, even if that root node
                 * is now UNUSED. Root nodes don't have a buddy so we can't merge with one.
                 */
                if (Parent_is_split(i) || bucket == bucket_limit)
                {
                    break;
                }

                /*
                 * If we get here, we know our buddy is UNUSED. In this case we should
                 * merge with that buddy and continue traversing up to the root node. We
                 * need to remove the buddy from its free list here but we don't need to
                 * add the merged parent to its free list yet. That will be done once after
                 * this loop is finished.
                 */
                List_remove((List_t*)Ptr_for_node(((i - 1) ^ 1) + 1, bucket));
                i = (i - 1) / 2;
                bucket--;
            }

            /*
             * Add ourselves to the free list for our bucket. We add to the back of the
             * list because "malloc" takes from the back of the list and we want a "free"
             * followed by a "malloc" of the same size to ideally use the same address
             * for better memory locality.
             */
            List_push(&buckets[bucket], (List_t*)Ptr_for_node(i, bucket));
        }

        //TODO: External
        protected abstract bool Brk(void* addr);

        //TODO: External
        protected abstract void* Sbrk(uint size);

        public struct Page
        {
            public byte bucket;
            public bool parent_is_split;
            public void* ptr;
        }

    }

}
