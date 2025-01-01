using System;

namespace LogJoint
{
    /// <summary>
    /// A change notifications hub. Data owners can post change notifications
    /// when their data changes. Data consumers can subscribe to be notified
    /// about the changes.
    /// The notifications are unspecific - they mean "something has changed".
    /// It's subscriber's job to check if what it's interested in has actually changed.
    /// The subscribes are notified asynchronously. When data owner posts
    /// a notification, the subscribes will be notified eventually, but
    /// not right away down the call chain of <see cref="IChangeNotification.Post"/>.
    /// </summary>
    public interface IChangeNotification
    {
        /// <summary>
        /// Notifies that some observable data has changed. Only data owner
        /// (usually the object that holds data variable) should detect changes and post
        /// notifications. Objects that expose computed data should not detect changes
        /// and post notifications.
        /// Thread-safe.
        /// </summary>
        void Post();
        /// <summary>
        /// An event that notifies that something has changed. It is not specific.
        /// It's listener's job to determine if change affect the data it's interested in.
        /// To do that the listener can read the data and compare with last seen.
        /// Many <see cref="IChangeNotification.Post"/> calls can correspond to one 
        /// event occurrence.
        /// Events can be temporarily suppressed for some <see cref="IChangeNotification"/> objects. 
        /// See <see cref="CreateChainedChangeNotification(bool)"/>.
        /// Event in fired in model synchronization context. See <see cref="ISynchronizationContext"/>.
        /// To have event subscription as disposable object use <see cref="CreateSubscription(Action, bool)"/>.
        /// </summary>
        event EventHandler OnChange;
        /// <summary>
        /// Determines if this <see cref="IChangeNotification"/> object fires <see cref="OnChange"/> events.
        /// Note that the events can be suppressed temporarily. <seealso cref="IChainedChangeNotification.Active"/>.
        /// </summary>
        bool IsEmittingEvents { get; }
        /// <summary>
        /// Subscribes for <see cref="OnChange"/> event and represents the subscription as disposable object.
        /// The subscription can be temporarily suppressed. See <see cref="ISubscription.Active"/>.
        /// </summary>
        /// <param name="sideEffect">A delegate that will be called when <see cref="OnChange"/> if fired.
        /// See <see cref="ISubscription.SideEffect"/></param>
        /// <param name="initiallyActive">Determines if subscription is active initially.
        /// See <see cref="ISubscription.Active"/>.</param>
        /// <returns></returns>
        ISubscription CreateSubscription(Action sideEffect, bool initiallyActive = true);
        /// <summary>
        /// Creates a <see cref="IChangeNotification"/> object that acts as this object,
        /// except it can be suppressed independently of this object.
        /// </summary>
        /// <param name="initiallyActive">Determines whether the new <see cref="IChainedChangeNotification"/>
        /// is initially active (not suppressed)</param>
        /// <returns></returns>
        IChainedChangeNotification CreateChainedChangeNotification(bool initiallyActive = true);
    };

    /// <summary>
    /// Represents one subscription to <see cref="IChangeNotification.OnChange"/> event.
    /// </summary>
    public interface ISubscription : IDisposable
    {
        /// <summary>
        /// Determines if subscription is active. Inactive subscription does not call <see cref="SideEffect"/> delegate event when 
        /// underlying <see cref="IChangeNotification.OnChange"/> is fired.
        /// </summary>
        bool Active { get; set; }
        /// <summary>
        /// A delegate that is called when <see cref="IChangeNotification.OnChange"/> is fired and when this subscription is active.
        /// </summary>
        Action SideEffect { get; set; }
    };

    /// <summary>
    /// Object that delegate functionality to another <see cref="IChangeNotification"/>, but
    /// can be suppressed independently of the original <see cref="IChangeNotification"/>.
    /// </summary>
    public interface IChainedChangeNotification : IChangeNotification, IDisposable
    {
        /// <summary>
        /// Determines whether this object suppresses its <see cref="IChangeNotification.OnChange"/> events or not.
        /// This property reflects the state of this object only. If this object is active but linked to 
        /// another suppressed <see cref="IChainedChangeNotification"/>, this property will still be true.
        /// Use <see cref="IChangeNotification.IsEmittingEvents"/> to check if whole chain is active 
        /// and <see cref="IChangeNotification.OnChange"/> events are actually raised.
        /// </summary>
        bool Active { get; set; }
    };
}
