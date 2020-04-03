using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum HandToTrack
{
    Left,
    Right
}

public class VRHandDraw : MonoBehaviour
{
    [SerializeField]
    private HandToTrack handToTrack = HandToTrack.Left;

    [SerializeField]
    private GameObject objectToTrackMovement;

    [SerializeField, Range(0, 1.0f)]
    private float minDistanceBeforeNewPoint = 0.2f;

    [SerializeField, Range(0, 1.0f)]
    private float minIndexFingerPinchValue = 0.5f;
    
    private Vector3 prevPointDistance = Vector3.zero;

    [SerializeField]
    private float lineDefaultWidth = 0.010f;

    private int positionCount = 0;

    private List<LineRenderer> lines = new List<LineRenderer>();

    private LineRenderer currentLineRenderer;

    [SerializeField]
    private Color defaultColor = Color.white;

    [SerializeField]
    private GameObject editorObjectToTrackMovement;

    [SerializeField]
    private bool allowEditorControls = false;

    [SerializeField]
    private Material defaultLineMaterial;

    private bool IsPinchReleased = false;

#region Oculus Types

    private OVRHand ovrHand;

    private OVRSkeleton ovrSkeleton;

    private OVRBone boneToTrack;

#endregion

    void Awake() 
    {
        ovrHand = objectToTrackMovement.GetComponent<OVRHand>();
        ovrSkeleton = objectToTrackMovement.GetComponent<OVRSkeleton>();

        #if UNITY_EDITOR

        if(allowEditorControls)
        {
            objectToTrackMovement = editorObjectToTrackMovement != null ? 
                editorObjectToTrackMovement : objectToTrackMovement;
        }

        #endif

        boneToTrack = ovrSkeleton.Bones
            .Where(b => b.Id == OVRSkeleton.BoneId.Hand_Index3)
            .SingleOrDefault();

        // add line renderer
        AddNewLineRenderer();
    }

    void AddNewLineRenderer()
    {
        positionCount = 0;
        // name of game object LineRenderer_left_1, LineRenderer_left_2, 
        // name of game object LineRenderer_right_1, LineRenderer_right_2
        GameObject go = new GameObject($"LineRenderer_{handToTrack.ToString()}_{lines.Count}");
        go.transform.parent = objectToTrackMovement.transform.parent;
        go.transform.position = objectToTrackMovement.transform.position;

        LineRenderer goLineRenderer = go.AddComponent<LineRenderer>();
        goLineRenderer.startWidth = lineDefaultWidth;
        goLineRenderer.endWidth = lineDefaultWidth;
        goLineRenderer.useWorldSpace = true;
        goLineRenderer.material = defaultLineMaterial;
        goLineRenderer.positionCount = 1;
        goLineRenderer.numCapVertices = 5;

        currentLineRenderer = goLineRenderer;
        lines.Add(goLineRenderer);
    }

    void Update()
    {
        // make sure that we are tracking a bone just in case
        // we don't have hand tracking information when the hame starts
        if(boneToTrack == null)
        {
            boneToTrack = ovrSkeleton.Bones
                .Where(b => b.Id == OVRSkeleton.BoneId.Hand_Index3)
                .SingleOrDefault();

            objectToTrackMovement = boneToTrack.Transform.gameObject;
        }
        
        CheckForPinchState();
    }

    void CheckForPinchState()
    {
        // tell us if we are currently pinching with our index finger
        bool isIndexFingerPinching = ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        // tell us the value of the pinch
        float indexFingerPinchValue = ovrHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        // tell us if the finger confidence is high
        if(ovrHand.GetFingerConfidence(OVRHand.HandFinger.Index) != OVRHand.TrackingConfidence.High)
        {
            return;
        }

        // finger pinch down
        if(isIndexFingerPinching && indexFingerPinchValue >= minIndexFingerPinchValue)
        {
            UpdateLine();
            IsPinchReleased = true;
            return;
        }

        // finger pinch up
        if(IsPinchReleased)
        {
            AddNewLineRenderer();
            IsPinchReleased = false;
        }
    }

    void UpdateLine()
    {
        if(prevPointDistance == null)
        {
            prevPointDistance = objectToTrackMovement.transform.position;
        }

        if(prevPointDistance != null && 
            Mathf.Abs(Vector3.Distance(prevPointDistance, objectToTrackMovement.transform.position)) 
            >= minDistanceBeforeNewPoint)
            {
                prevPointDistance = objectToTrackMovement.transform.position;
                AddPoint(prevPointDistance);
            }
    }

    void AddPoint(Vector3 position)
    {
        currentLineRenderer.SetPosition(positionCount, position);
        positionCount++;
        currentLineRenderer.positionCount = positionCount + 1;
        currentLineRenderer.SetPosition(positionCount, position);
    }
}
