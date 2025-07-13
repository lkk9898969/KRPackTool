using System.Threading;

namespace KartRider.Common.Utilities;

public sealed class LockFreeQueue<T>
    where T : class
{
    private SingleLinkNode mHead;

    private SingleLinkNode mTail;

    public LockFreeQueue()
    {
        mHead = new SingleLinkNode();
        mTail = mHead;
    }

    public T Next => mHead.Next == null ? default : mHead.Next.Item;

    private static bool CompareAndExchange(ref SingleLinkNode pLocation, SingleLinkNode pComparand,
        SingleLinkNode pNewValue)
    {
        return pComparand == Interlocked.CompareExchange(ref pLocation, pNewValue, pComparand);
    }

    public bool Dequeue(out T pItem)
    {
        bool flag;
        pItem = default;
        SingleLinkNode singleLinkNode = null;
        var flag1 = false;
        while (true)
            if (!flag1)
            {
                singleLinkNode = mHead;
                var singleLinkNode1 = mTail;
                var next = singleLinkNode.Next;
                if (singleLinkNode == mHead)
                {
                    if (singleLinkNode != singleLinkNode1)
                    {
                        pItem = next.Item;
                        flag1 = CompareAndExchange(ref mHead, singleLinkNode, next);
                    }
                    else if (next != null)
                    {
                        CompareAndExchange(ref mTail, singleLinkNode1, next);
                    }
                    else
                    {
                        flag = false;
                        break;
                    }
                }
            }
            else
            {
                flag = true;
                break;
            }

        return flag;
    }

    public T Dequeue()
    {
        T t;
        Dequeue(out t);
        return t;
    }

    public void Enqueue(T pItem)
    {
        SingleLinkNode singleLinkNode = null;
        var singleLinkNode1 = new SingleLinkNode
        {
            Item = pItem
        };
        var flag = false;
        while (!flag)
        {
            singleLinkNode = mTail;
            var next = singleLinkNode.Next;
            if (mTail == singleLinkNode)
            {
                if (next != null)
                    CompareAndExchange(ref mTail, singleLinkNode, next);
                else
                    flag = CompareAndExchange(ref mTail.Next, null, singleLinkNode1);
            }
        }

        CompareAndExchange(ref mTail, singleLinkNode, singleLinkNode1);
    }

    private class SingleLinkNode
    {
        public T Item;
        public SingleLinkNode Next;
    }
}