// ----------------------------------------------------------------------------
// <copyright file="CustomTypes.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// Sets up support for Unity-specific types. Can be a blueprint how to register your own Custom Types for sending.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

#if UNITY_4_7 || UNITY_5 || UNITY_5_3_OR_NEWER
#define SUPPORTED_UNITY
#endif

#if SUPPORTED_UNITY
namespace Photon.Realtime
{
    using Photon.Realtime;
    using ExitGames.Client.Photon;
    using UnityEngine;
    using Debug = UnityEngine.Debug;


    /// <summary>
    /// Internally used class, containing de/serialization methods for various Unity-specific classes.
    /// Adding those to the Photon serialization protocol allows you to send them in events, etc.
    /// </summary>
    internal static class CustomTypesUnity
    {
        /// <summary>Register de/serializer methods for Unity specific types. Makes the types usable in RaiseEvent and PUN.</summary>
        internal static void Register()
        {
            PhotonPeer.RegisterType(typeof(Vector2), (byte) 'W', SerializeVector2, DeserializeVector2);
            PhotonPeer.RegisterType(typeof(Vector3), (byte) 'V', SerializeVector3, DeserializeVector3);
            PhotonPeer.RegisterType(typeof(Quaternion), (byte) 'Q', SerializeQuaternion, DeserializeQuaternion);
        }


        #region Custom De/Serializer Methods

        public static readonly byte[] memVector3 = new byte[3 * 4];

        private static short SerializeVector3(StreamBuffer outStream, object customobject)
        {
            Vector3 vo = (Vector3) customobject;

            int index = 0;
            lock (memVector3)
            {
                byte[] bytes = memVector3;
                Protocol.Serialize(vo.x, bytes, ref index);
                Protocol.Serialize(vo.y, bytes, ref index);
                Protocol.Serialize(vo.z, bytes, ref index);
                outStream.Write(bytes, 0, 3 * 4);
            }

            return 3 * 4;
        }

        private static object DeserializeVector3(StreamBuffer inStream, short length)
        {
            Vector3 vo = new Vector3();
            lock (memVector3)
            {
                inStream.Read(memVector3, 0, 3 * 4);
                int index = 0;
                Protocol.Deserialize(out vo.x, memVector3, ref index);
                Protocol.Deserialize(out vo.y, memVector3, ref index);
                Protocol.Deserialize(out vo.z, memVector3, ref index);
            }

            return vo;
        }


        public static readonly byte[] memVector2 = new byte[2 * 4];

        private static short SerializeVector2(StreamBuffer outStream, object customobject)
        {
            Vector2 vo = (Vector2) customobject;
            lock (memVector2)
            {
                byte[] bytes = memVector2;
                int index = 0;
                Protocol.Serialize(vo.x, bytes, ref index);
                Protocol.Serialize(vo.y, bytes, ref index);
                outStream.Write(bytes, 0, 2 * 4);
            }

            return 2 * 4;
        }

        private static object DeserializeVector2(StreamBuffer inStream, short length)
        {
            Vector2 vo = new Vector2();
            lock (memVector2)
            {
                inStream.Read(memVector2, 0, 2 * 4);
                int index = 0;
                Protocol.Deserialize(out vo.x, memVector2, ref index);
                Protocol.Deserialize(out vo.y, memVector2, ref index);
            }

            return vo;
        }


        public static readonly byte[] memQuarternion = new byte[4 * 4];

        private static short SerializeQuaternion(StreamBuffer outStream, object customobject)
        {
            Quaternion o = (Quaternion) customobject;

            lock (memQuarternion)
            {
                byte[] bytes = memQuarternion;
                int index = 0;
                Protocol.Serialize(o.w, bytes, ref index);
                Protocol.Serialize(o.x, bytes, ref index);
                Protocol.Serialize(o.y, bytes, ref index);
                Protocol.Serialize(o.z, bytes, ref index);
                outStream.Write(bytes, 0, 4 * 4);
            }

            return 4 * 4;
        }

        private static object DeserializeQuaternion(StreamBuffer inStream, short length)
        {
            Quaternion o = new Quaternion();

            lock (memQuarternion)
            {
                inStream.Read(memQuarternion, 0, 4 * 4);
                int index = 0;
                Protocol.Deserialize(out o.w, memQuarternion, ref index);
                Protocol.Deserialize(out o.x, memQuarternion, ref index);
                Protocol.Deserialize(out o.y, memQuarternion, ref index);
                Protocol.Deserialize(out o.z, memQuarternion, ref index);
            }

            return o;
        }

        #endregion
    }
}
#endif