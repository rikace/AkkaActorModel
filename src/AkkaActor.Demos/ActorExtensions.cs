namespace AkkaActor.Demos
{
    using Akka.Actor;
    using Akka.Cluster.Tools.Singleton;
   
    public static class ActorExtensions
    {
        public static IActorRef BiztalkFacadeRef = ActorRefs.Nobody;

        /// <summary>
        /// Creates a cluster singleton actor given its <see cref="Props"/>. Singleton actor will
        /// exists only on one node in a cluster at a time. If this node goes down, it will be 
        /// recreated on another one (usually it lives on the oldest node in the cluster).
        /// </summary>
        /// <param name="system"></param>
        /// <param name="props"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IActorRef SingletonOf(this ActorSystem system, Props props, string name)
        {
            return system.ActorOf(ClusterSingletonManager.Props(
                singletonProps: props,
                settings: ClusterSingletonManagerSettings.Create(system).WithRole(name)), name);
        }

        /// <summary>
        /// Creates a cluster singleton proxy. Unlike <see cref="SingletonOf"/>, it will not create
        /// an actor, but it can be used to communicate with cluster singleton without need to route
        /// to it. In case of transition i.e. when cluster singleton will migrate from one node
        /// to another, proxy will buffer the messages.
        /// </summary>
        /// <param name="system"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IActorRef SingletonProxy(this ActorSystem system, string name)
        {
            return system.ActorOf(ClusterSingletonProxy.Props(
                singletonManagerPath: $"/user/{name}",
                settings: ClusterSingletonProxySettings.Create(system).WithRole(name)), $"{name}-proxy");
        }
    }
}
