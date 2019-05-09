namespace Enzyme.Visiting
{
    interface IAcceptVisitor
    {
        unsafe int Accept<TVisitor>(byte* payload, ref TVisitor visitor)
            where TVisitor : struct, IVisitor;
    }

    interface IAcceptLimitedLengthVisitor
    {
        unsafe void Accept<TVisitor>(byte* payload, int length, ref TVisitor visitor)
            where TVisitor : struct, IVisitor;
    }
}