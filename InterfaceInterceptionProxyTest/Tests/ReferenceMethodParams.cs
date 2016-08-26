using System;
using InterfaceInterceptionProxy;
using NSubstitute;
using NUnit.Framework;

namespace InterfaceInterceptionProxyTest
{
    public class ReferenceMethodParams
    {
        [Test]
        public void BehaviourNotChangedWhenNoInterceptionOut()
        {
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IOutTest), typeof(OutNoInterceptionTest));

            var testInstance = new OutNoInterceptionTest();
            var proxy = (IOutTest)Activator.CreateInstance(proxyType, new object[] { testInstance });
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

            int @out;
            testInstance.Out = random.Next();
            proxy.OutTest(out @out);

            Assert.IsTrue(@out == testInstance.Out);
        }

        [Test]
        public void BehaviourNotChangedWhenNoInterceptionRef()
        {
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IRefTest), typeof(RefNoInterceptionTest));

            var testInstance = new RefNoInterceptionTest();
            var proxy = (IRefTest)Activator.CreateInstance(proxyType, new object[] { testInstance });
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);

            testInstance.Ref = random.Next();
            var @ref = 10;
            proxy.RefTest(ref @ref);

            Assert.IsTrue(@ref == testInstance.Ref);
        }

        [Test]
        public void TestOutParamInterception()
        {
            var handler = Substitute.For<IInterceptionHandler>();

            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();

            handler.InterceptingAction(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).Returns(x => { Assert.IsTrue(((ParamInfo[])x[1])[0].IsOut); return ((ParamInfo[])x[1])[0].Value = val; });
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IOutTest), typeof(OutpTest));
            var proxy = (IOutTest)Activator.CreateInstance(proxyType, new object[] { new OutpTest(), handler });

            var @out = random.Next();
            proxy.OutTest(out @out);

            Assert.IsTrue(@out == val);
        }

        [Test]
        public void TestRefParamInterception()
        {
            var handler = Substitute.For<IInterceptionHandler>();

            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();

            handler.InterceptingAction(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).Returns(x => { Assert.IsTrue(((ParamInfo[])x[1])[0].IsByRef); return ((ParamInfo[])x[1])[0].Value = val; });
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(IRefTest), typeof(RefpTest));
            var proxy = (IRefTest)Activator.CreateInstance(proxyType, new object[] { new RefpTest(), handler });

            var @ref = random.Next();
            proxy.RefTest(ref @ref);

            Assert.IsTrue(@ref == val);
        }
    }
}