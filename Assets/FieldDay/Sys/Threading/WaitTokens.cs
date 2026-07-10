using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Data;
using System;
using System.Collections;
using UnityEngine;

namespace FieldDay.Threading {
    public abstract class WaitToken : IEnumerator, IDisposable {
        #region Abstract

        public abstract bool MoveNext();
        public abstract void Dispose();

        #endregion // Abstract

        #region Private Interface Implementation

        object IEnumerator.Current { get { return null; } }
        void IEnumerator.Reset() { throw new NotSupportedException(); }

        #endregion // Private Interface Implementation
    }

    static public partial class WaitTokens {
        static private IPool<IdAllocatorToken> s_IdAllocatorTokenPool;
        static private IPool<PredicateToken> s_PredicateTokenPool;

        #region Lifetime

        static internal void Initialize() {
            if (EngineHints.TryGetHintInt("POOL_WAIT_TOKENS_ID_FIXED", out int fixedCapacity)) {
                s_IdAllocatorTokenPool = new FixedPool<IdAllocatorToken>(fixedCapacity, (p) => new IdAllocatorToken(p));
            } else {
                int dynCapacity = EngineHints.GetHintInt("POOL_WAIT_TOKENS_ID", 8);
                s_IdAllocatorTokenPool = new DynamicPool<IdAllocatorToken>(dynCapacity, (p) => new IdAllocatorToken(p));
            }
            s_PredicateTokenPool = new DynamicPool<PredicateToken>(16, (p) => new PredicateToken(p));

            s_IdAllocatorTokenPool.Prewarm();
            s_PredicateTokenPool.Prewarm();
        }

        static internal void Shutdown() {
            s_IdAllocatorTokenPool.Dispose();
            s_PredicateTokenPool.Dispose();
        }

        #endregion // Lifetime

        #region Id Allocators

        /// <summary>
        /// Returns a WaitToken that waits until the given id is freed.
        /// </summary>
        static public WaitToken WhileIdAllocated(UniqueId16 id, UniqueIdAllocator16 allocator) {
            Assert.NotNull(allocator);

            if (!allocator.IsValid(id)) {
                return null;
            }

            IdAllocatorToken token = s_IdAllocatorTokenPool.Alloc();
            token.Initialize(id, allocator);
            return token;
        }

        /// <summary>
        /// Returns a WaitToken that waits until the given id is freed.
        /// </summary>
        static public WaitToken WhileIdAllocated(UniqueId32 id, UniqueIdAllocator32 allocator) {
            Assert.NotNull(allocator);

            if (!allocator.IsValid(id)) {
                return null;
            }

            IdAllocatorToken token = s_IdAllocatorTokenPool.Alloc();
            token.Initialize(id, allocator);
            return token;
        }

        #endregion // Id Allocators

        #region Predicates

        /// <summary>
        /// Returns a WaitToken that waits until the given predicate returns true.
        /// </summary>
        static public WaitToken WhileNotPredicate(Func<bool> predicate) {
            Assert.NotNull(predicate);

            if (predicate()) {
                return null;
            }

            PredicateToken token = s_PredicateTokenPool.Alloc();
            token.Initialize(predicate);
            return token;
        }

        #endregion // Predicates
    }

    internal sealed class IdAllocatorToken : WaitToken {
        private enum Mode : uint {
            None,
            Id16,
            Id32
        }

        private readonly IPool<IdAllocatorToken> m_Pool;
        private Mode m_Mode;
        private uint m_Id;
        private object m_Allocator;

        public IdAllocatorToken(IPool<IdAllocatorToken> pool) {
            m_Pool = pool;
        }

        public void Initialize(UniqueId16 id, UniqueIdAllocator16 allocator) {
            m_Mode = Mode.Id16;
            m_Id = id.Id;
            m_Allocator = allocator;
        }

        public void Initialize(UniqueId32 id, UniqueIdAllocator32 allocator) {
            m_Mode = Mode.Id32;
            m_Id = id.Id;
            m_Allocator = allocator;
        }

        public override unsafe bool MoveNext() {
            uint id = m_Id;
            switch(m_Mode) {
                case Mode.Id16: {
                    return Unsafe.FastCast<UniqueIdAllocator16>(m_Allocator).IsValid(*(UniqueId16*)(&id));;
                }
                case Mode.Id32: {
                    return Unsafe.FastCast<UniqueIdAllocator32>(m_Allocator).IsValid(*(UniqueId32*)(&id)); ;
                }
                default: {
                    return false;
                }
            }
        }

        public override void Dispose() {
            if (m_Allocator != null) {
                m_Allocator = null;
                m_Mode = 0;
                m_Pool.Free(this);
            }
        }
    }

    internal sealed class PredicateToken : WaitToken {
        private readonly IPool<PredicateToken> m_Pool;
        private Func<bool> m_Predicate;

        public PredicateToken(IPool<PredicateToken> pool) {
            m_Pool = pool;
        }

        public void Initialize(Func<bool> predicate) {
            m_Predicate = predicate;
        }

        public override unsafe bool MoveNext() {
            return m_Predicate != null && !m_Predicate();
        }

        public override void Dispose() {
            if (m_Predicate != null) {
                m_Predicate = null;
                m_Pool.Free(this);
            }
        }
    }
}