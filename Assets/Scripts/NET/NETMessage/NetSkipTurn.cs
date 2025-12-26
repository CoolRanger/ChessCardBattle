using Unity.Collections;
using Unity.Networking.Transport;

public class NetSkipTurn : NetMessage
{

    public NetSkipTurn()
    {
        Code = OpCode.SKIP_STEP;
    }

    public NetSkipTurn(DataStreamReader reader)
    {
        Code = OpCode.SKIP_STEP;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        // no data to deserialize
    }

    public override void ReceivedOnClient()
    {
        NetUtility.C_SKIP_STEP?.Invoke(this);
    }

    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.S_SKIP_STEP?.Invoke(this, cnn);
    }
}