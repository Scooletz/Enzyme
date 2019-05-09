using System;

namespace Enzyme.Visiting
{
    public interface IVisitor
    {
        void PushField(byte fieldNumber);
        void PopField();

        void On(bool value);
        void On(byte value);
        void On(sbyte value);
        void On(short value);
        void On(ushort value);
        void On(int value);
        void On(uint value);
        void On(long value);
        void On(ulong value);
        void On(DateTime value);
        void On(Guid value);
        void On(Span<byte> utf8);
        void OnNull();
    }
}