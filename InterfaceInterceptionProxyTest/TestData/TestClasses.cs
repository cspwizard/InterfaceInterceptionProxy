using System;

namespace InterfaceInterceptionProxyTest
{
    public interface IGenericTest<T>
    {
        T Property { get; set; }

        T Function(T param);
    }

    public interface ITest
    {
        int Sum(int a, int b);
    }

    internal class TestClass : ITest
    {
        [TestInterceptor]
        public int Sum(int a, int b)
        {
            return a + b;
        }
    }

    internal class TestClassMultiIntercept : ITest
    {
        [TestInterceptor]
        [TestSecondInterceptor]
        public int Sum(int a, int b)
        {
            return a + b;
        }
    }

    internal class TestClassMultiIntercept2Handlers : ITest
    {
        [TestInterceptor]
        [TestNewInterceptor]
        public int Sum(int a, int b)
        {
            return a + b;
        }
    }

    public interface ITestVoid
    {
        void Sum(int a, int b);
    }

    internal class TestClassVoid : ITestVoid
    {
        [TestInterceptor]
        public void Sum(int a, int b)
        {
        }
    }

    internal class TestClassVoidNoInterceptor : ITestVoid
    {
        public void Sum(int a, int b)
        {
        }
    }

    internal class TestClassExplicitImplementation : ITest
    {
        [TestInterceptor]
        int ITest.Sum(int a, int b)
        {
            return a + b;
        }
    }

    internal class TestClassExplicitImplementationNoInterceptor : ITest
    {
        int ITest.Sum(int a, int b)
        {
            return a + b;
        }
    }

    internal class TestClassNoInterceptor : ITest
    {
        public int Sum(int a, int b)
        {
            return a + b;
        }
    }

    internal class TestGeneric<T> : IGenericTest<T>
    {
        public T Property { get; set; }

        [TestInterceptor]
        public T Function(T param)
        {
            Property = param;
            return Property;
        }
    }

    internal class TestGenericNoInterception<T> : IGenericTest<T>
    {
        public T Property { get; set; }

        public T Function(T param)
        {
            Property = param;
            return Property;
        }
    }

    public interface IOutTest
    {
        int OutTest(out int @out);
    }

    internal class OutpTest : IOutTest
    {
        public int Out { get; set; }

        [TestInterceptor]
        public int OutTest(out int @out)
        {
            return @out = Out;
        }
    }

    internal class OutNoInterceptionTest : IOutTest
    {
        public int Out { get; set; }

        public int OutTest(out int @out)
        {
            return @out = Out;
        }
    }

    public interface IRefTest
    {
        int RefTest(ref int @ref);
    }

    internal class RefpTest : IRefTest
    {
        public int Ref { get; set; }

        [TestInterceptor]
        public int RefTest(ref int @ref)
        {
            return @ref = Ref;
        }
    }

    internal class RefNoInterceptionTest : IRefTest
    {
        public int Ref { get; set; }

        public int RefTest(ref int @ref)
        {
            return @ref = Ref;
        }
    }

    public interface IGenericMethodTest
    {
        Tuple<T,I> Function<T, I>(T itemt, I itemi);
    }

    internal class TestGenericMethod : IGenericMethodTest
    {
        [TestInterceptor]
        public Tuple<T, I> Function<T, I>(T itemt, I itemi)
        {
            return new Tuple<T, I>(itemt, itemi);
        }
    }

    internal class TestGenericMethodNoInterception : IGenericMethodTest
    {
        public Tuple<T, I> Function<T, I>(T itemt, I itemi)
        {
            return new Tuple<T, I>(itemt, itemi);
        }
    }
}