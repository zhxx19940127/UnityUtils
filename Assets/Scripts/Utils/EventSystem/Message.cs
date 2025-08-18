using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class Message
{
    // type - methods
    private static readonly Dictionary<Type, List<MessageEvent>> _classType2Methods =
        new Dictionary<Type, List<MessageEvent>>();

    private static Message _instance = new Message();

    // instance - methods
    private readonly Dictionary<object, List<MessageEvent>> _subscribeInstance2Methods =
        new Dictionary<object, List<MessageEvent>>();

    // tag - methods
    private readonly Dictionary<string, List<MessageEvent>> _subscribeTag2Methods =
        new Dictionary<string, List<MessageEvent>>();

    private readonly Dictionary<Type, string> _type2Tag = new Dictionary<Type, string>();

    private List<string> Filterate = new List<string>();

    public static Message DefaultEvent => _instance ?? (_instance = new Message());

#if UNITY_EDITOR
    private bool isInit = false;
#endif
    /// <summary>
    ///     Post parameters to all subscibed methods
    ///     将参数广播到全部监听方法
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="parameters"></param>
    public void Post(string tag, params object[] parameters)
    {
#if UNITY_EDITOR
        if (!isInit)
        {
            var list = MessageHelper.GetFilterMessageName();
            Filterate.AddRange(list);
            isInit = true;
        }

        if (!Filterate.Contains(tag))
            Debug.Log($"PostMessage====>:{tag}");
#endif
        if (!_subscribeTag2Methods.TryGetValue(tag, out var todo)) return;
        if (todo.Count == 0) return;

        var executeEvent = new List<MessageEvent>();
        foreach (var td in todo)
        {
            if (td.Tag != tag) continue;
            try
            {
                executeEvent.Add(td);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        foreach (var messageEvent in executeEvent) messageEvent.Invoke(parameters);
    }

    public void Post<T>(T inMessageData) where T : class, IMessageData
    {
        var type = typeof(T);

        if (!_type2Tag.TryGetValue(type, out var tag))
        {
            tag = typeof(T).FullName;
            _type2Tag[type] = typeof(T).FullName;
            if (string.IsNullOrEmpty(tag))
            {
                tag = typeof(T).FullName;
                _type2Tag[type] = typeof(T).FullName;
            }
        }

        Post(tag, inMessageData);
    }


    /// <summary>
    ///     Unregister all subscribed methods in a type
    ///     取消注册某类型中全部被监听方法
    /// </summary>
    /// <param name="val"></param>
    public void Unregister<T>(T val) where T : class
    {
        if (!_subscribeInstance2Methods.TryGetValue(val, out var methods)) return;

        var tmpMethods = new List<MessageEvent>();
        tmpMethods.AddRange(methods);
        foreach (var method in tmpMethods) UnregisterOneMethod(method.Tag, val);
    }

    /// <summary>
    ///     默认是一个instance同一个tag只注册一个方法
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="instance"></param>
    public void UnregisterOneMethod(string tag, object instance)
    {
// #if UNITY_EDITOR
//             if (!Filterate.Contains(tag))
//                 L.M(LE.MESSAGE, "UNREGISTER=>" + $"{tag}");
// #endif

        if (_subscribeTag2Methods.TryGetValue(tag, out var values))
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (values[i].Instance != instance) continue;
                values.RemoveAt(i);
                break;
            }

            if (values.Count <= 0) _subscribeTag2Methods.Remove(tag);
        }

        if (_subscribeInstance2Methods.TryGetValue(instance, out var events))
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Tag != tag) continue;
                events.RemoveAt(i);
                break;
            }

            if (events.Count <= 0) _subscribeInstance2Methods.Remove(instance);
        }
    }

    public void Register<T>(T val) where T : class
    {
        var type = val.GetType();

        if (_subscribeInstance2Methods.ContainsKey(val))
        {
            Debug.LogWarning($"{type.FullName}已注册");
            return;
        }


        //如果没有缓存
        if (!_classType2Methods.TryGetValue(type, out var events))
        {
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            events = new List<MessageEvent>();
            foreach (var method in methods)
            {
                var methodAttr = method.GetCustomAttributes(typeof(SubscriberAttribute), false);
                var HasAttr = methodAttr.Length > 0;
                if (!HasAttr) continue;
                if (methodAttr[0] is SubscriberAttribute subscriberAttribute)
                {
                    var tags = new List<string>();
                    if (subscriberAttribute.Tags != null)
                    {
                        foreach (var tag in ((SubscriberAttribute)methodAttr[0]).Tags)
                            tags.Add(tag);
                    }
                    else
                    {
                        var paramsInfos = method.GetParameters();
                        for (var i = 0; i < paramsInfos.Length; i++)
                        {
                            var paramsInfo = paramsInfos[i];
                            var paramsType = paramsInfo.ParameterType;
                            var isMessageData = paramsType.GetInterfaces().Contains(typeof(IMessageData));
                            if (isMessageData)
                            {
                                var tag1 = paramsType.ToString();
                                if (!tags.Contains(tag1)) tags.Add(tag1);
                            }
                        }
                    }

                    for (var i = 0; i < tags.Count; i++)
                        events.Add(new MessageEvent(method as MethodInfo, val, tags[i]));
                }
            }

            _classType2Methods[type] = events;
        }

        foreach (var messageEvent in events)
        {
            var msgEvent = new MessageEvent(messageEvent, val);
            Register(msgEvent);
        }
    }

    public void Register<T>(T val, Action method, string tag) where T : class
    {
        Register(new MessageEvent(o => method(), val, tag));
    }

    public void Register<T, P>(T val, Action<P> method, string tag) where T : class
    {
        Register(new MessageEvent(o => method((P)(o as object[])[0]), val, tag));
    }

    public void Register<T, P, P2>(T val, Action<P, P2> method, string tag) where T : class
    {
        Register(new MessageEvent(o =>
        {
            var paras = o as object[];
            method((P)paras[0], (P2)paras[1]);
        }, val, tag));
    }

    public void Register<T, P, P2, P3>(T val, Action<P, P2, P3> method, string tag) where T : class
    {
        Register(new MessageEvent(o =>
        {
            var paras = o as object[];
            method((P)paras[0], (P2)paras[1], (P3)paras[2]);
        }, val, tag));
    }

    public void Register<T, P, P2, P3, P4>(T val, Action<P, P2, P3, P4> method, string tag) where T : class
    {
        Register(new MessageEvent(o =>
        {
            var paras = o as object[];
            method((P)paras[0], (P2)paras[1], (P3)paras[2], (P4)paras[3]);
        }, val, tag));
    }

    public void Register<T, P, P2, P3, P4, P5>(T val, Action<P, P2, P3, P4, P5> method, string tag)
        where T : class
    {
        Register(new MessageEvent(o =>
        {
            var paras = o as object[];
            method((P)paras[0], (P2)paras[1], (P3)paras[2], (P4)paras[3], (P5)paras[4]);
        }, val, tag));
    }

    private void Register(MessageEvent messageEvent)
    {
// #if UNITY_EDITOR
//             if (!Filterate.Contains(messageEvent.Tag))
//                 L.M(LE.MESSAGE, "REGISTER=>" + $"{messageEvent.Tag}");
// #endif
        if (!_subscribeInstance2Methods.TryGetValue(messageEvent.Instance, out var instanceEvents))
        {
            instanceEvents = new List<MessageEvent>();
            _subscribeInstance2Methods[messageEvent.Instance] = instanceEvents;
        }

        instanceEvents.Add(messageEvent);

        if (!_subscribeTag2Methods.TryGetValue(messageEvent.Tag, out var paraTypeEvents))
        {
            paraTypeEvents = new List<MessageEvent>();
            _subscribeTag2Methods[messageEvent.Tag] = paraTypeEvents;
        }

        paraTypeEvents.Add(messageEvent);
    }

    public void Clear()
    {
        _subscribeInstance2Methods.Clear();
        _classType2Methods.Clear();
        _subscribeTag2Methods.Clear();
    }

    private struct MessageEvent
    {
        private readonly Action<object, object> action;
        public readonly object Instance;
        public readonly string Tag;

        public MessageEvent(MessageEvent messageEvent, object instance)
        {
            action = messageEvent.action;
            Tag = messageEvent.Tag;
            Instance = instance;
        }

        public MessageEvent(MethodInfo info, object instance, string tag)
        {
            Instance = instance;
            Tag = tag;
            action = (ins, o) => info.Invoke(ins, o as object[]);
        }


        public MessageEvent(Action<object> action, object instance, string tag)
        {
            this.action = (ins, o) => action(o);
            Instance = instance;
            Tag = tag;
        }

        public void Invoke(params object[] para)
        {
            try
            {
                action(Instance, para);
            }
            catch (Exception e)
            {
                Debug.LogError($"执行方法出错=>{Instance.GetType().Name} {Tag} {e}");
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class SubscriberAttribute : Attribute
{
    public SubscriberAttribute()
    {
    }

    public SubscriberAttribute(string tag)
    {
        Tags = new[] { tag };
    }

    public SubscriberAttribute(string tag1, string tag2)
    {
        Tags = new[] { tag1, tag2 };
    }

    public SubscriberAttribute(string tag1, string tag2, string tag3)
    {
        Tags = new[] { tag1, tag2, tag3 };
    }

    public string[] Tags { get; }
}