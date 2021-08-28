using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WoodMeshCut : MonoBehaviour
{

    private int[] meshTriangles;
    private Vector3[] meshVertices;
    private Vector3[] meshNormals;

    //Create new Mesh based on original board
    public void ComputeSlice(Plane plane, Mesh mesh, bool posSide) {

      meshTriangles = mesh.triangles;
      meshVertices = mesh.vertices;
      meshNormals = mesh.normals;

      //Loop through each triangle in mesh
      for (int i=0; i<meshTriangles.Length; i+=3) {

        //Vert 1
        Vector3 v1 = meshVertices[meshTriangles[i]];
        bool v1Side = plane.GetSide(v1);

        //Vert 2
        Vector3 v2 = meshVertices[meshTriangles[i+1]];
        bool v2Side = plane.GetSide(v2);

        //Vert 3
        Vector3 v3 = meshVertices[meshTriangles[i+2]];
        bool v3Side = plane.GetSide(v3);


        //See if triangle is intersected by plane
        if (v1Side != v2Side || v2Side != v3Side) {

          //Condition 1: V1 and V2 are on the same side of the plane
          if (v1Side == v2Side) {

            //Get the intersection of plane from 3 to 2
            Vector3 intersection = GetPlaneIntersection(v3, v2, plane);

            //Condition 1.1: Planes pos side and v3 are on different sides
            if (posSide != v3Side) {

              Vector3 refVector = v3;

              //Move all vectors that are equal to v3
              for (int j=0; j<meshVertices.Length; j++) {
                if (meshVertices[j] == refVector) {
                  meshVertices[j] = intersection;
                }
              }

            //Condition 1.2: Planes pos side and v3 are on the same side
            } else {

              Vector3 refVector = v2;

              //Move all vectors that are equal to v2
              for (int j=0; j<meshVertices.Length; j++) {
                if (meshVertices[j] == refVector) {
                  meshVertices[j] = intersection;
                }
              }
            }
          }

          //Condition 2: V2 and V3 are on the same side of the plane
          else if (v2Side == v3Side) {

            //Get the intersection of plane from 1 to 2
            Vector3 intersection = GetPlaneIntersection(v1, v3, plane);

            //Condition 2.1: Planes pos side and v1 are on the different sides
            if (posSide != v1Side) {

              Vector3 refVector = v1;

              //Move all vectors that are equal to v1
              for (int j=0; j<meshVertices.Length; j++) {
                if (meshVertices[j] == refVector) {
                  meshVertices[j] = intersection;
                }
              }

            //Condition 2.1: Planes pos side and v1 are on the same side
            } else {

              Vector3 refVector = v3;

              //Move all vectors that are equal to v3
              for (int j=0; j<meshVertices.Length; j++) {
                if (meshVertices[j] == refVector) {
                  meshVertices[j] = intersection;
                }
              }
            }
          }
        }
      }


      //Instantiate new board and apply new mesh
      GameObject cutBoard = Instantiate(gameObject, transform.position, transform.rotation);
      Mesh boardMesh = cutBoard.GetComponent<MeshFilter>().mesh;

      boardMesh.vertices = meshVertices;
      boardMesh.RecalculateNormals();
      boardMesh.RecalculateBounds();

      //Reset Box Collider
      Destroy(cutBoard.GetComponent<BoxCollider>());
      BoxCollider collider = cutBoard.AddComponent<BoxCollider>();

      //Reset XRGrabInteractable
      cutBoard.GetComponent<XRGrabInteractable>().colliders[0] = collider;
    }


    //Get Intersection point on plane between two vectors
    private Vector3 GetPlaneIntersection(Vector3 vA, Vector3 vB, Plane plane) {

      float dist;
      Ray ray = new Ray(vA, (vB - vA));
      plane.Raycast(ray, out dist);
      return ray.GetPoint(dist);
    }



























    /* ORIGINAL CONCEPT
    public void ComputeCut(Vector3 contactPoint) {

      Vector3 vA, vB;
      float cutOffset = 0.005f;

      List<int> triangleIndexs = new List<int>();
      triangleIndexs = DetermineFaceTriangles(contactPoint, out vA, out vB);

      //Establish start cut points
      Vector3 heading = vB - vA;
      float mag = heading.magnitude - cutOffset;
      heading.Normalize();

      Vector3 lhs = contactPoint - vA;
      float product = Vector3.Dot(lhs, heading);
      product = Mathf.Clamp(product, 0f, mag);

      Vector3 startCutPointLeft = vA + heading * (product + cutOffset);
      Vector3 startCutPointRight = vA + heading * (product - cutOffset);

      //Test: spawn test points
      // Instantiate(testPoint, startCutPointLeft, Quaternion.identity);
      // Instantiate(testPoint, startCutPointRight, Quaternion.identity);

      //Create cut point
      Vector3 cross = Vector3.Cross(meshNormals[meshTriangles[triangleIndexs[0]]], vB - vA);
      cross.Normalize();

      float cutLength = 0.1f;
      Vector3 cutPoint = contactPoint + (cross * cutLength);

      //Test: spawn test point
      // Instantiate(testPoint, cutPoint, Quaternion.identity);

      //Establish triangles using new points
      List<int> verticePoints = new List<int>();

      for (int i=0; i < triangleIndexs.Count; i++) {

        verticePoints.Add(meshTriangles[triangleIndexs[i]]);
        verticePoints.Add(meshTriangles[triangleIndexs[i] +1]);
        verticePoints.Add(meshTriangles[triangleIndexs[i] +2]);
      }

      RemoveTriangles(triangleIndexs);
      BuildNewTriangles(verticePoints, cutPoint, startCutPointLeft, startCutPointRight);

      //Apply to mesh
      mesh.vertices = meshVertices;
      mesh.triangles = meshTriangles;
      mesh.RecalculateNormals();

    }


    private List<int> DetermineFaceTriangles(Vector3 contactPoint, out Vector3 vA, out Vector3 vB) {

      List<int> triangleFaces = new List<int>();
      int closestTriangle = -1;
      float closestDist = 100f;
      float dist;

      vA = Vector3.zero;
      vB = Vector3.zero;

      //Loop through trianges and find the triangle that is the closest to the contact point
      for (int i=0; i < meshTriangles.Length; i += 3) {

        Vector3 v1 = transform.TransformPoint(meshVertices[meshTriangles[i]]);
        Vector3 v2 = transform.TransformPoint(meshVertices[meshTriangles[i+1]]);
        Vector3 v3 = transform.TransformPoint(meshVertices[meshTriangles[i+2]]);

        dist = ClosestPointOnLine(contactPoint, v1, v2);

        if (dist < closestDist) {
          vA = v1;
          vB = v2;
          closestDist = dist;
          closestTriangle = i;
        }

        dist = ClosestPointOnLine(contactPoint, v2, v3);

        if (dist < closestDist) {
          vA = v2;
          vB = v3;
          closestDist = dist;
          closestTriangle = i;
        }

        dist = ClosestPointOnLine(contactPoint, v1, v3);

        if (dist < closestDist) {
          vA = v1;
          vB = v3;
          closestDist = dist;
          closestTriangle = i;
        }
      }

      triangleFaces.Add(closestTriangle);

      //Find other triangle on the same face

      for (int i=0; i < meshTriangles.Length; i += 3) {

        if (i != triangleFaces[0]) {

          //Check normals
          if (meshNormals[meshTriangles[triangleFaces[0]]] == meshNormals[meshTriangles[i]]) {

            triangleFaces.Add(i);
          }
        }
      }

      return triangleFaces;
    }


    private float ClosestPointOnLine(Vector3 vPoint, Vector3 vA, Vector3 vB) {

      Vector3 heading = vB - vA;
      float mag = heading.magnitude;
      heading.Normalize();

      Vector3 lhs = vPoint - vA;
      float product = Vector3.Dot(lhs, heading);
      product = Mathf.Clamp(product, 0f, mag);

      Vector3 closestPoint = vA + heading * product;

      return Vector3.Distance(vPoint, closestPoint);
    }


    private void RemoveTriangles(List<int> triangleIndexs) {

      for (int i=0; i < triangleIndexs.Count; i++) {
        meshTriangles[triangleIndexs[i]] = -1;
        meshTriangles[triangleIndexs[i] + 1] = -1;
        meshTriangles[triangleIndexs[i] + 2] = -1;
      }

      List<int> meshTrianglesList = new List<int>(meshTriangles);

      meshTrianglesList.RemoveAll(list_item => list_item == -1);

      meshTriangles = meshTrianglesList.ToArray();
    }


    private void BuildNewTriangles(List<int> verticePoints, Vector3 cutPoint, Vector3 startCutPointLeft, Vector3 startCutPointRight) {

      cutPoint = transform.InverseTransformPoint(cutPoint);
      startCutPointLeft = transform.InverseTransformPoint(startCutPointLeft);
      startCutPointRight = transform.InverseTransformPoint(startCutPointRight);

      Instantiate(testPoint, cutPoint, Quaternion.identity);
      Instantiate(testPoint, startCutPointLeft, Quaternion.identity);
      Instantiate(testPoint, startCutPointRight, Quaternion.identity);

      List<Vector3> meshVerticesList = new List<Vector3>(meshVertices);
      List<int> meshTrianglesList = new List<int>(meshTriangles);

      //Add cutPoint, CPL, CPR to vertices
      meshVerticesList.Add(cutPoint);
      int cutPointIndex = meshVerticesList.Count -1;

      meshVerticesList.Add(startCutPointLeft);
      int startCutPointLeftIndex = meshVerticesList.Count -1;

      meshVerticesList.Add(startCutPointRight);
      int startCutPointRightIndex = meshVerticesList.Count -1;

      //Triangle1:
      meshTrianglesList.Add(verticePoints[0]);
      meshTrianglesList.Add(cutPointIndex);
      meshTrianglesList.Add(verticePoints[5]);

      //Triangle2:
      meshTrianglesList.Add(verticePoints[3]);
      meshTrianglesList.Add(verticePoints[1]);
      meshTrianglesList.Add(cutPointIndex);

      //Triangle3:
      meshTrianglesList.Add(cutPointIndex);
      meshTrianglesList.Add(verticePoints[1]);
      meshTrianglesList.Add(startCutPointRightIndex);

      //Triangle4:
      meshTrianglesList.Add(cutPointIndex);
      meshTrianglesList.Add(startCutPointLeftIndex);
      meshTrianglesList.Add(verticePoints[4]);

      //Triangle5:
      meshTrianglesList.Add(verticePoints[5]);
      meshTrianglesList.Add(cutPointIndex);
      meshTrianglesList.Add(verticePoints[2]);

      //Update Mesh;
      meshVertices = meshVerticesList.ToArray();
      meshTriangles = meshTrianglesList.ToArray();

    }
    */
}
