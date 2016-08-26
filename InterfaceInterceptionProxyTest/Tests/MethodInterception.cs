using System;
using InterfaceInterceptionProxy;
using NSubstitute;
using NUnit.Framework;

namespace InterfaceInterceptionProxyTest
{
    public class TypedMethodOverload
    {
        [Test]
        public void BehaviourNotChangedWhenNoInterceptionGenericMethod()
        {
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IGenericMethodTest), typeof(TestGenericMethodNoInterception));

            var testInstance = new TestGenericMethodNoInterception();
            var proxy = (IGenericMethodTest)Activator.CreateInstance(proxyType, new object[] { testInstance });
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();
            var res = proxy.Function(val, val);

            Assert.IsTrue(res.Item1 == val && res.Item2 == val);
        }

        [Test]
        public void BehaviourNotChangedWhenNoInterceptionTyped()
        {
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITest), typeof(TestClassNoInterceptor));

            var testInstance = new TestClassNoInterceptor();
            var proxy = (ITest)Activator.CreateInstance(proxyType, new object[] { testInstance });
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();

            Assert.IsTrue(proxy.Sum(val, 5) == testInstance.Sum(val, 5));
        }

        [Test]
        public void BehaviourNotChangedWhenNoInterceptionVoid()
        {
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITestVoid), typeof(TestClassVoidNoInterceptor));
            var testInstance = new TestClassVoidNoInterceptor();
            var proxy = (ITestVoid)Activator.CreateInstance(proxyType, new object[] { testInstance });
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            proxy.Sum(10, 5);
        }

        [Test]
        public void TestInterceptsGenericMethod()
        {
            var handler = Substitute.For<IInterceptionHandler>();
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();
            handler.InterceptingAction<Tuple<int, int>>(Arg.Any<TDelegate<Tuple<int, int>>>(), Arg.Any<ParamInfo[]>()).ReturnsForAnyArgs(new Tuple<int, int>(val, val));
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IGenericMethodTest), typeof(TestGenericMethod));
            var proxy = (IGenericMethodTest)Activator.CreateInstance(proxyType, new object[] { new TestGenericMethod(), handler });
            var res = proxy.Function(random.Next(), random.Next());

            handler.Received().InterceptingAction<Tuple<int, int>>(Arg.Any<TDelegate<Tuple<int, int>>>(), Arg.Any<ParamInfo[]>());
            Assert.IsTrue(res.Item1 == val && res.Item2 == val);
        }

        [Test]
        public void TestInterceptsTyped()
        {
            var handler = Substitute.For<IInterceptionHandler>();
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();
            handler.InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).ReturnsForAnyArgs(val);
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITest), typeof(TestClass));
            var proxy = (ITest)Activator.CreateInstance(proxyType, new object[] { new TestClass(), handler });
            var sum = proxy.Sum(5, 5);

            handler.Received().InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>());
            Assert.IsTrue(sum == val);
        }

        [Test]
        public void TestInterceptsTypedParamInfoIsCorrect()
        {
            var handler = Substitute.For<IInterceptionHandler>();
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();
            handler.InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>())
                .Returns(x =>
                {
                    var @params = ((ParamInfo[])x[1]);
                    Assert.IsFalse(@params[0].IsByRef);
                    Assert.IsFalse(@params[1].IsByRef);
                    Assert.IsFalse(@params[0].IsOut);
                    Assert.IsFalse(@params[1].IsOut);
                    Assert.AreEqual(@params[0].Type, typeof(int));
                    Assert.AreEqual(@params[1].Type, typeof(int));
                    Assert.AreEqual(@params[0].Name, "a");
                    Assert.AreEqual(@params[1].Name, "b");
                    Assert.AreEqual(@params[0].Value, 5);
                    Assert.AreEqual(@params[1].Value, 5);
                    return val;
                });

            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITest), typeof(TestClass));
            var proxy = (ITest)Activator.CreateInstance(proxyType, new object[] { new TestClass(), handler });
            var sum = proxy.Sum(5, 5);

            handler.Received().InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>());
            Assert.IsTrue(sum == val);
        }

        [Test]
        public void TestInterceptsVoid()
        {
            var handler = Substitute.For<IInterceptionHandler>();
            handler.InterceptingAction(Arg.Any<VoidDelegate>(), Arg.Any<ParamInfo[]>());
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITestVoid), typeof(TestClassVoid));
            var proxy = (ITestVoid)Activator.CreateInstance(proxyType, new object[] { new TestClassVoid(), handler });

            proxy.Sum(5, 5);
            handler.Received().InterceptingAction(Arg.Any<VoidDelegate>(), Arg.Any<ParamInfo[]>());
        }
    }
}