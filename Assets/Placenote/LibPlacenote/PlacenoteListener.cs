using System;
using UnityEngine;

public interface PlacenoteListener
{
	void OnInitialized(bool success, string errMsg);
	void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose);
	void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus);
	void OnDensePointcloud(LibPlacenote.PNFeaturePointUnity[] ptcloud);
}

