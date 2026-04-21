using BeauUtil;
using FieldDay.SharedState;
using System;

namespace FieldDay.Systems {
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public sealed class SysUpdateAttribute : Attribute {
        public readonly SysUpdate Info;

        public SysUpdateAttribute(GameLoopPhase phase, int order = 0, int mask = Bits.All32) {
            Info = new SysUpdate(phase, order, mask);
        }

        public SysUpdateAttribute(GameLoopPhaseMask phase, int order = 0, int mask = Bits.All32) {
            Info = new SysUpdate(phase, order, mask);
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SystemModuleShim : SystemComponent {
        protected SysUpdate GetUpdate() {
            return Reflect.GetAttribute<SysUpdateAttribute>(GetType())?.Info ?? SysUpdate.Default();
        }
        protected abstract SysPermissions GetPermissions();

        protected unsafe abstract delegate*<float, void> GetDelegate();

        public override unsafe void RegisterSystems(ref SystemRegistrationTable ecs) {
            ecs.Register(GetDelegate(),
                GetUpdate(),
                GetPermissions());
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SharedStateSystemBehaviour<TSharedA> : SystemModuleShim
        where TSharedA : class, ISharedState
    {
        static protected TSharedA m_StateA;

        static protected void GetDependencies() {
            Find.State(out m_StateA);
        }

        protected override SysPermissions GetPermissions() {
            return new SysPermissions()
                .ReadWriteShared<TSharedA>();
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SharedStateSystemBehaviour<TSharedA, TSharedB> : SystemModuleShim
        where TSharedA : class, ISharedState
        where TSharedB : class, ISharedState
    {
        static protected TSharedA m_StateA;
        static protected TSharedB m_StateB;

        static protected void GetDependencies() {
            Find.State(out m_StateA, out m_StateB);
        }

        protected override SysPermissions GetPermissions() {
            return new SysPermissions()
                .ReadWriteShared<TSharedA>()
                .ReadWriteShared<TSharedB>();
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SharedStateSystemBehaviour<TSharedA, TSharedB, TSharedC> : SystemModuleShim
        where TSharedA : class, ISharedState
        where TSharedB : class, ISharedState
        where TSharedC : class, ISharedState
    {
        static protected TSharedA m_StateA;
        static protected TSharedB m_StateB;
        static protected TSharedC m_StateC;

        static protected void GetDependencies() {
            Find.State(out m_StateA, out m_StateB, out m_StateC);
        }

        protected override SysPermissions GetPermissions() {
            return new SysPermissions()
                .ReadWriteShared<TSharedA>()
                .ReadWriteShared<TSharedB>()
                .ReadWriteShared<TSharedC>();
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SharedStateSystemBehaviour<TSharedA, TSharedB, TSharedC, TSharedD> : SystemModuleShim
        where TSharedA : class, ISharedState
        where TSharedB : class, ISharedState
        where TSharedC : class, ISharedState
        where TSharedD : class, ISharedState
    {
        static protected TSharedA m_StateA;
        static protected TSharedB m_StateB;
        static protected TSharedC m_StateC;
        static protected TSharedD m_StateD;

        static protected void GetDependencies() {
            Find.State(out m_StateA, out m_StateB, out m_StateC, out m_StateD);
        }

        protected override SysPermissions GetPermissions() {
            return new SysPermissions()
                .ReadWriteShared<TSharedA>()
                .ReadWriteShared<TSharedB>()
                .ReadWriteShared<TSharedC>()
                .ReadWriteShared<TSharedD>();
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SharedStateSystemBehaviour<TSharedA, TSharedB, TSharedC, TSharedD, TSharedE> : SystemModuleShim
        where TSharedA : class, ISharedState
        where TSharedB : class, ISharedState
        where TSharedC : class, ISharedState
        where TSharedD : class, ISharedState
        where TSharedE : class, ISharedState
    {
        static protected TSharedA m_StateA;
        static protected TSharedB m_StateB;
        static protected TSharedC m_StateC;
        static protected TSharedD m_StateD;
        static protected TSharedE m_StateE;

        static protected void GetDependencies() {
            Find.State(out m_StateA, out m_StateB, out m_StateC, out m_StateD);
            Find.State(out m_StateE);
        }

        protected override SysPermissions GetPermissions() {
            return new SysPermissions()
                .ReadWriteShared<TSharedA>()
                .ReadWriteShared<TSharedB>()
                .ReadWriteShared<TSharedC>()
                .ReadWriteShared<TSharedD>()
                .ReadWriteShared<TSharedE>();
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SharedStateSystemBehaviour<TSharedA, TSharedB, TSharedC, TSharedD, TSharedE, TSharedF> : SystemModuleShim
        where TSharedA : class, ISharedState
        where TSharedB : class, ISharedState
        where TSharedC : class, ISharedState
        where TSharedD : class, ISharedState
        where TSharedE : class, ISharedState
        where TSharedF : class, ISharedState
    {
        static protected TSharedA m_StateA;
        static protected TSharedB m_StateB;
        static protected TSharedC m_StateC;
        static protected TSharedD m_StateD;
        static protected TSharedE m_StateE;
        static protected TSharedF m_StateF;

        static protected void GetDependencies() {
            Find.State(out m_StateA, out m_StateB, out m_StateC, out m_StateD);
            Find.State(out m_StateE, out m_StateF);
        }

        protected override SysPermissions GetPermissions() {
            return new SysPermissions()
                .ReadWriteShared<TSharedA>()
                .ReadWriteShared<TSharedB>()
                .ReadWriteShared<TSharedC>()
                .ReadWriteShared<TSharedD>()
                .ReadWriteShared<TSharedE>()
                .ReadWriteShared<TSharedF>();
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SharedStateSystemBehaviour<TSharedA, TSharedB, TSharedC, TSharedD, TSharedE, TSharedF, TSharedG> : SystemModuleShim
        where TSharedA : class, ISharedState
        where TSharedB : class, ISharedState
        where TSharedC : class, ISharedState
        where TSharedD : class, ISharedState
        where TSharedE : class, ISharedState
        where TSharedF : class, ISharedState
        where TSharedG : class, ISharedState
    {
        static protected TSharedA m_StateA;
        static protected TSharedB m_StateB;
        static protected TSharedC m_StateC;
        static protected TSharedD m_StateD;
        static protected TSharedE m_StateE;
        static protected TSharedF m_StateF;
        static protected TSharedG m_StateG;

        static protected void GetDependencies() {
            Find.State(out m_StateA, out m_StateB, out m_StateC, out m_StateD);
            Find.State(out m_StateE, out m_StateF, out m_StateG);
        }

        protected override SysPermissions GetPermissions() {
            return new SysPermissions()
                .ReadWriteShared<TSharedA>()
                .ReadWriteShared<TSharedB>()
                .ReadWriteShared<TSharedC>()
                .ReadWriteShared<TSharedD>()
                .ReadWriteShared<TSharedE>()
                .ReadWriteShared<TSharedF>()
                .ReadWriteShared<TSharedG>();
        }
    }

    [Obsolete("This is using the old version of ECS Systems. Please rework to the new standard when you can.", !Game.IsDevBuild)]
    public abstract class SharedStateSystemBehaviour<TSharedA, TSharedB, TSharedC, TSharedD, TSharedE, TSharedF, TSharedG, TSharedH> : SystemModuleShim
        where TSharedA : class, ISharedState
        where TSharedB : class, ISharedState
        where TSharedC : class, ISharedState
        where TSharedD : class, ISharedState
        where TSharedE : class, ISharedState
        where TSharedF : class, ISharedState
        where TSharedG : class, ISharedState
        where TSharedH : class, ISharedState
    {
        static protected TSharedA m_StateA;
        static protected TSharedB m_StateB;
        static protected TSharedC m_StateC;
        static protected TSharedD m_StateD;
        static protected TSharedE m_StateE;
        static protected TSharedF m_StateF;
        static protected TSharedG m_StateG;
        static protected TSharedH m_StateH;

        static protected void GetDependencies() {
            Find.State(out m_StateA, out m_StateB, out m_StateC, out m_StateD);
            Find.State(out m_StateE, out m_StateF, out m_StateG, out m_StateH);
        }

        protected override SysPermissions GetPermissions() {
            return new SysPermissions()
                .ReadWriteShared<TSharedA>()
                .ReadWriteShared<TSharedB>()
                .ReadWriteShared<TSharedC>()
                .ReadWriteShared<TSharedD>()
                .ReadWriteShared<TSharedE>()
                .ReadWriteShared<TSharedF>()
                .ReadWriteShared<TSharedG>()
                .ReadWriteShared<TSharedH>();
        }
    }
}