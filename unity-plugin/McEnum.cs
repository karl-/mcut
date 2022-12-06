// ReSharper disable InconsistentNaming
// ReSharper disable BuiltInTypeReferenceStyleForMemberAccess
using System;

namespace MeshCut
{
    public enum McResult
    {
        // The function was successfully executed.
        MC_NO_ERROR = 0, 
        // An internal operation could not be executed successively.
        MC_INVALID_OPERATION = -(1 << 1), 
        // An invalid value has been passed to the API.
        MC_INVALID_VALUE = -(1 << 2), 
        // Memory allocation operation cannot allocate memory.
        MC_OUT_OF_MEMORY = -(1 << 3), 
        // Wildcard (match all) .
        MC_RESULT_MAX_ENUM = Int32.MaxValue 
    }

    public enum McDispatchFlags
    {
        // Interpret the input mesh vertices as arrays of 32-bit floating-point numbers
        MC_DISPATCH_VERTEX_ARRAY_FLOAT = (1 << 0),
        // Interpret the input mesh vertices as arrays of 64-bit floating-point numbers
        MC_DISPATCH_VERTEX_ARRAY_DOUBLE = (1 << 1),
        // Require that all intersection paths/curves/contours partition the source-mesh into two (or more) fully disjoint parts. Otherwise, ::mcDispatch is a no-op. This flag enforces the requirement that only through-cuts are valid cuts i.e it disallows partial cuts. NOTE: This flag may not be used with ::MC_DISPATCH_FILTER_FRAGMENT_LOCATION_UNDEFINED
        MC_DISPATCH_REQUIRE_THROUGH_CUTS = (1 << 2),
        // Compute connected-component-to-input mesh vertex-id maps. See also: ::MC_CONNECTED_COMPONENT_DATA_VERTEX_MAP 
        MC_DISPATCH_INCLUDE_VERTEX_MAP = (1 << 3),
        // Compute connected-component-to-input mesh face-id maps. . See also: ::MC_CONNECTED_COMPONENT_DATA_FACE_MAP
        MC_DISPATCH_INCLUDE_FACE_MAP = (1 << 4),

        //
        // Compute fragments that are above the cut-mesh
        MC_DISPATCH_FILTER_FRAGMENT_LOCATION_ABOVE = (1 << 5),
        // Compute fragments that are below the cut-mesh
        MC_DISPATCH_FILTER_FRAGMENT_LOCATION_BELOW = (1 << 6),
        // Compute fragments that are partially cut i.e. neither above nor below the cut-mesh. NOTE: This flag may not be used with ::MC_DISPATCH_REQUIRE_THROUGH_CUTS. 
        MC_DISPATCH_FILTER_FRAGMENT_LOCATION_UNDEFINED = (1 << 7),

        //
        // Compute fragments that are fully sealed (hole-filled) on the interior.   
        MC_DISPATCH_FILTER_FRAGMENT_SEALING_INSIDE = (1 << 8),
        // Compute fragments that are fully sealed (hole-filled) on the exterior.  
        MC_DISPATCH_FILTER_FRAGMENT_SEALING_OUTSIDE = (1 << 9),

        //
        // Compute fragments that are not sealed (holes not filled
        MC_DISPATCH_FILTER_FRAGMENT_SEALING_NONE = (1 << 10),

        //
        // Compute patches on the inside of the source mesh (those used to fill holes
        MC_DISPATCH_FILTER_PATCH_INSIDE = (1 << 11),
        // Compute patches on the outside of the source mesh
        MC_DISPATCH_FILTER_PATCH_OUTSIDE = (1 << 12),

        //
        // Compute the seam which is the same as the source-mesh but with new edges placed along the cut path. Note: a seam from the source-mesh will only be computed if the dispatch operation computes a complete (through) cut
        MC_DISPATCH_FILTER_SEAM_SRCMESH = (1 << 13),
        // Compute the seam which is the same as the cut-mesh but with new edges placed along the cut path. Note: a seam from the cut-mesh will only be computed if the dispatch operation computes a complete (through) cut
        MC_DISPATCH_FILTER_SEAM_CUTMESH = (1 << 14),

        //
        // Keep all connected components resulting from the dispatched cut.
        MC_DISPATCH_FILTER_ALL = ( //
            MC_DISPATCH_FILTER_FRAGMENT_LOCATION_ABOVE | //
            MC_DISPATCH_FILTER_FRAGMENT_LOCATION_BELOW | //
            MC_DISPATCH_FILTER_FRAGMENT_LOCATION_UNDEFINED | //
            MC_DISPATCH_FILTER_FRAGMENT_SEALING_INSIDE | //
            MC_DISPATCH_FILTER_FRAGMENT_SEALING_OUTSIDE | //
            MC_DISPATCH_FILTER_FRAGMENT_SEALING_NONE | //
            MC_DISPATCH_FILTER_PATCH_INSIDE | //
            MC_DISPATCH_FILTER_PATCH_OUTSIDE | //
            MC_DISPATCH_FILTER_SEAM_SRCMESH | //
            MC_DISPATCH_FILTER_SEAM_CUTMESH),
        /** 
         * Allow MCUT to perturb the cut-mesh if the inputs are not in general position. 
         * 
         * MCUT is formulated for inputs in general position. Here the notion of general position is defined with
        respect to the orientation predicate (as evaluated on the intersecting polygons). Thus, a set of points 
        is in general position if no three points are collinear and also no four points are coplanar.

        MCUT uses the "GENERAL_POSITION_VIOLATION" flag to inform of when to use perturbation (of the
        cut-mesh) so as to bring the input into general position. In such cases, the idea is to solve the cutting
        problem not on the given input, but on a nearby input. The nearby input is obtained by perturbing the given
        input. The perturbed input will then be in general position and, since it is near the original input,
        the result for the perturbed input will hopefully still be useful.  This is justified by the fact that
        the task of MCUT is not to decide whether the input is in general position but rather to make perturbation
        on the input (if) necessary within the available precision of the computing device. */
        MC_DISPATCH_ENFORCE_GENERAL_POSITION = (1 << 15)
    }

    public enum McConnectedComponentType : UInt32
    {
        // A connected component that originates from the source-mesh. 
        MC_CONNECTED_COMPONENT_TYPE_FRAGMENT = (1 << 0),

        // A connected component that is originates from the cut-mesh. 
        MC_CONNECTED_COMPONENT_TYPE_PATCH = (1 << 2),

        // A connected component representing an input mesh (source-mesh or cut-mesh), but with additional vertices and
        // edges that are introduced as as a result of the cut (i.e. the intersection contour/curve). 
        MC_CONNECTED_COMPONENT_TYPE_SEAM = (1 << 3),

        // A connected component that is copy of an input mesh (source-mesh or cut-mesh). Such a connected component may
        // contain new faces and vertices, which will happen if MCUT internally performs polygon partitioning. Polygon
        // partitioning occurs when an input mesh intersects the other without severing at least one edge. An example is
        // splitting a tetrahedron (source-mesh) in two parts using one large triangle (cut-mesh): in this case, the
        // large triangle would be partitioned into two faces to ensure that at least one of this cut-mesh are severed
        // by the tetrahedron. This is what allows MCUT to reconnect topology after the cut. 
        MC_CONNECTED_COMPONENT_TYPE_INPUT = (1 << 4),

        // Wildcard (match all) . 
        MC_CONNECTED_COMPONENT_TYPE_ALL = 0xFFFFFFFF
    }
}
