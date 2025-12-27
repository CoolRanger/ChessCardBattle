using Unity.Collections;
using Unity.Networking.Transport;

public class NetPlayerLeft : NetMessage
{
    public NetPlayerLeft()
    {
        Code = OpCode.PLAYER_LEFT;
    }

    public NetPlayerLeft(DataStreamReader reader)
    {
        Code = OpCode.PLAYER_LEFT;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        
    }

    public override void ReceivedOnClient()
    {
        NetUtility.C_PLAYER_LEFT?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_PLAYER_LEFT?.Invoke(this, cnn);
    }
}