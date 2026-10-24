// Sprite shader for world objects. The sprite (_MainTex) only provides the shape through its alpha,
// while the colour comes from _WorldTex sampled in world space, so neighbouring objects of the same
// material tile seamlessly. Meshes can lock the texture by supplying world coordinates in UV0.zw
// (sprite meshes only have UV0.xy, so no extra vertex channel is required).
Shader "TopDown/World Texture"
{
    Properties
    {
        [PerRendererData] _MainTex ("Shape Sprite", 2D) = "white" {}
        _WorldTex ("World Texture", 2D) = "white" {}
        _WorldTexPixelsPerUnit ("World Texture Pixels Per Unit", Float) = 64
        [Toggle] _WorldTexUseMeshCoords ("Use Mesh UV0.zw As World Coordinates", Float) = 0
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _WorldTex;
            float4 _WorldTex_TexelSize;
            float _WorldTexPixelsPerUnit;
            float _WorldTexUseMeshCoords;
            fixed4 _Color;

            struct VertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                // xy: sprite shape uv, zw: optional locked world coordinates
                float4 shapeUv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 shapeUv : TEXCOORD0;
                float2 worldUv : TEXCOORD1;
            };

            VertexOutput Vert(VertexInput input)
            {
                VertexOutput output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.shapeUv = input.shapeUv.xy;
                output.color = input.color * _Color;

                float2 worldCoords = mul(unity_ObjectToWorld, input.vertex).xy;
                worldCoords = lerp(worldCoords, input.shapeUv.zw, _WorldTexUseMeshCoords);
                output.worldUv = worldCoords * _WorldTexPixelsPerUnit * _WorldTex_TexelSize.xy;
                return output;
            }

            fixed4 Frag(VertexOutput input) : SV_Target
            {
                fixed shapeAlpha = tex2D(_MainTex, input.shapeUv).a;
                fixed4 colour = tex2D(_WorldTex, frac(input.worldUv)) * input.color;
                colour.a *= shapeAlpha;
                colour.rgb *= colour.a;
                return colour;
            }
            ENDCG
        }
    }
}
