Shader "Unlit/ARCameraGrayScaleShader"
{
	Properties
	{
        _colorScale("Scale", Float) = 0.5
    	_textureY ("TextureY", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
            ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			float4x4 _DisplayTransform;

			struct Vertex
			{
				float4 position : POSITION;
				float2 texcoord : TEXCOORD0;

			};

			struct TexCoordInOut
			{
				float4 position : SV_POSITION;
				float2 texcoord : TEXCOORD0;
			};

			TexCoordInOut vert (Vertex vertex)
			{
				TexCoordInOut o;
				o.position = UnityObjectToClipPos(vertex.position); 

				float texX = vertex.texcoord.x;
				float texY = vertex.texcoord.y;
				
				o.texcoord.x = (_DisplayTransform[0].x * texX + _DisplayTransform[1].x * (texY) + _DisplayTransform[2].x);
 			 	o.texcoord.y = (_DisplayTransform[0].y * texX + _DisplayTransform[1].y * (texY) + (_DisplayTransform[2].y));
	            
				return o;
			}
			
            // samplers
        	float _colorScale;
            sampler2D _textureY;
            sampler2D _textureCbCr;

			fixed4 frag (TexCoordInOut i) : SV_Target
			{
				// sample the texture
                float2 texcoord = i.texcoord;
                float y = tex2D(_textureY, texcoord).r*_colorScale;
                return float4(y, y, y, 1.0);
			}
			ENDCG
		}
	}
}
