using System;
using InterfaceInterceptionProxy;
using NSubstitute;
using NUnit.Framework;

namespace InterfaceInterceptionProxyTest
{
    public class MultiInterception
    {
        [Test]
        public void TestInterceptsMultiWithMultiHandler()
        {
            var handler = Substitute.For<IInterceptionHandler>();
            var handler2 = Substitute.For<INewInterceptionHandler>();
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();
            handler.InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).ReturnsForAnyArgs(x => ((TDelegate<int>)x[0]).Invoke((ParamInfo[])x[1]));
            handler2.InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).ReturnsForAnyArgs(x => ((TDelegate<int>)x[0]).Invoke((ParamInfo[])x[1]));
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITest), typeof(TestClassMultiIntercept2Handlers));
            var proxy = (ITest)Activator.CreateInstance(proxyType, new object[] { new TestClassMultiIntercept2Handlers(), handler, handler2 });
            var sum = proxy.Sum(val, 5);

            handler.Received().InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>());
            handler2.Received().InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>());
            Assert.IsTrue(sum == val + 5);
        }

        [Test]
        public void TestInterceptsMultiWithOneHandler()
        {
            var handler = Substitute.For<IInterceptionHandler>();
            var random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            var val = random.Next();
            handler.InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).ReturnsForAnyArgs(x => ((TDelegate<int>)x[0]).Invoke((ParamInfo[])x[1]));

            //handler2.InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>()).ReturnsForAnyArgs(val);
            var proxyType = InterfaceBuilderStrategy.CreateInterfaceProxy(typeof(ITest), typeof(TestClassMultiIntercept));
            var proxy = (ITest)Activator.CreateInstance(proxyType, new object[] { new TestClassMultiIntercept(), handler });
            var sum = proxy.Sum(val, 5);

            handler.Received().InterceptingAction<int>(Arg.Any<TDelegate<int>>(), Arg.Any<ParamInfo[]>());
            Assert.IsTrue(sum == val + 5);
        }
    }
}