using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SawCut : MonoBehaviour
{

    [SerializeField]
    private GameObject bladeTip;

    [SerializeField]
    private GameObject bladeBottom;

    Vector3 bladeTipStartPos, bladeBottomStartPos, bladeTipEndPos;


    private void OnTriggerEnter(Collider col) {

      //Get the start position of the cut
      if (col.gameObject.tag == "Cuttable") {

        bladeTipStartPos = bladeTip.transform.position;
        bladeBottomStartPos = bladeBottom.transform.position;
      }
    }


    private void OnTriggerExit(Collider col) {

      if (col.gameObject.tag == "Cuttable") {

        //Get Exit Point
        bladeTipEndPos = bladeTip.transform.position;

        //Calculate Normal
        Vector3 side1 = bladeTipEndPos - bladeTipStartPos;
        Vector3 side2 = bladeTipEndPos - bladeBottomStartPos;
        Vector3 normal = Vector3.Cross(side1, side2).normalized;

        //Set to local space
        Vector3 colNormal = ((Vector3)(col.gameObject.transform.localToWorldMatrix.transpose * normal)).normalized;
        Vector3 colStartPoint = col.gameObject.transform.InverseTransformPoint(bladeTipStartPos);

        //Set up plane
        Plane plane = new Plane();
        plane.SetNormalAndPosition(colNormal, colStartPoint);

        //Check plane direction to keep consistant
        float direction = Vector3.Dot(Vector3.up, colNormal);

        if (direction < 0) {
          plane = plane.flipped;
        }

        //Get Mesh of board
        Mesh mesh = col.gameObject.GetComponent<MeshFilter>().mesh;

        //Duplicate board with altered meshs based on which side of the plane
        col.gameObject.GetComponent<WoodMeshCut>().ComputeSlice(plane, mesh, true);
        col.gameObject.GetComponent<WoodMeshCut>().ComputeSlice(plane, mesh, false);

        //Destroy board
        Destroy(col.gameObject);
      }
    }


















    /*ORIGINAL CONCEPT
    // private void OnCollisionEnter(Collision col) {
    //
    //   if (col.gameObject.tag == "Cuttable") {
    //     if (!cutting) {
    //
    //       cutting = true;
    //       rb.useGravity = false;
    //       rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    //       // rb.isKinematic = true;
    //
    //       Vector3 point = col.contacts[0].point;
    //       Vector3 closestPoint = col.collider.ClosestPoint(point);
    //
    //       //Test
    //       Instantiate(markerSphere, closestPoint, Quaternion.identity);
    //
    //       col.gameObject.GetComponent<WoodMeshCut>().ComputeCut(closestPoint);
    //     }
    //   }
    // }
    //
    //
    // private void OnCollisionExit(Collision col) {
    //
    //   if (col.gameObject.tag == "Cuttable") {
    //
    //     rb.useGravity = true;
    //     rb.constraints = RigidbodyConstraints.None;
    //     // rb.isKinematic = false;
    //   }
    // }
    */
}
