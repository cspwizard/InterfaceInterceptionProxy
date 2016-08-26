using System;
using InterfaceInterceptionProxy;

using NSubstitute;
using NUnit.Framework;

namespace InterfaceInterceptionProxyTest
{
    public class ExplicitImplementation
    {
        [Test]
        public void TestInterfaceExplicitImplementationNoInterception()
        {
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITest), typeof(TestClassExplicitImplementationNoInterceptor));
            var testInstance = new TestClassExplicitImplementationNoInterceptor();

            var proxy = (ITest)Activator.CreateInstance(proxyType, new object[] { testInstance });
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();

            Assert.IsTrue(proxy.Sum(val, 5) == ((ITest)testInstance).Sum(val, 5));
        }

        [Test]
        public void TestInterfaceExplicitImplementationWithInterception()
        {
            var handler = Substitute.For<IInterceptionHandler>();
            handler.InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).ReturnsForAnyArgs(0);

            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITest), typeof(TestClassExplicitImplementation));
            var proxy = (ITest)Activator.CreateInstance(proxyType, new object[] { new TestClassExplicitImplementation(), handler });

            Assert.IsTrue(0 == proxy.Sum(5, 5));
            handler.Received().InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>());
        }
    }
}