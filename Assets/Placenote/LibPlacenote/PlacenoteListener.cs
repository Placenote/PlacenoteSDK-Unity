using System;
using UnityEngine;
using System.Collections.Generic;

public interface PlacenoteListener
{
	void OnInitialized(bool success, string errMsg);
	void OnPose(Matrix4x4 outputPose, Matrix4x4 arkitPose);
	void OnStatusChange(LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus);
	void OnDensePointcloud(LibPlacenote.PNFeaturePointUnity[] ptcloud);
	void OnDenseMeshBlocks(Dictionary<LibPlacenote.PNMeshBlockIndex, LibPlacenote.PNMeshBlock> meshBlocks);
}

