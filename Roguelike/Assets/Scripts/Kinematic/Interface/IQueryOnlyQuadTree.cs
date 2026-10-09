using Kinematic.Data;
using System.Collections.Generic;

namespace Kinematic.Interface
{
    public interface IQueryOnlyQuadTree<T>
    {
        public void Query(in AABB queryBounds, List<T> results);
    }
}
