using Unity.Collections;
using Unity.Networking.Transport;

public class NetPromote : NetMessage
{
    public int x;
    public int y;
    public FixedString32Bytes newType;

    public NetPromote()
    {
        Code = OpCode.PROMOTE;
    }

    public NetPromote(DataStreamReader reader)
    {
        Code = OpCode.PROMOTE;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(x);
        writer.WriteInt(y);
        writer.WriteFixedString32(newType);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        x = reader.ReadInt();
        y = reader.ReadInt();
        newType = reader.ReadFixedString32();
    }

    public override void ReceivedOnClient()
    {
        NetUtility.C_PROMOTE?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_PROMOTE?.Invoke(this, cnn);
    }
}