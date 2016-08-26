using System;
using InterfaceInterceptionProxy;
using NSubstitute;
using NUnit.Framework;

namespace InterfaceInterceptionProxyTest
{
    public class GenericClassInterception
    {
        [Test]
        public void BehaviourNotChangedWhenNoInterception()
        {
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IGenericTest<int>), typeof(TestGenericNoInterception<int>));

            var testInstance = new TestGenericNoInterception<int>();
            var proxy = (IGenericTest<int>)Activator.CreateInstance(proxyType, new object[] { testInstance });
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();

            Assert.IsTrue(proxy.Function(val) == proxy.Property && proxy.Property == val);
        }

        [Test]
        public void TestIntercepts()
        {
            var handler = Substitute.For<IInterceptionHandler>();
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var retVal = random.Next();
            handler.InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).ReturnsForAnyArgs(retVal);
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IGenericTest<int>), typeof(TestGeneric<int>));
            var proxy = (IGenericTest<int>)Activator.CreateInstance(proxyType, new object[] { new TestGeneric<int>(), handler });
            var val = random.Next();

            Assert.IsTrue(proxy.Function(val) == retVal && proxy.Property == default(int));
            handler.Received().InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>());
        }
    }
}