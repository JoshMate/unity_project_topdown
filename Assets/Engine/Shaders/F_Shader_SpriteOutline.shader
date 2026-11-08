// Standard sprite shader with an optional pixel outline drawn around the sprite's opaque shape.
// The outline is toggled per renderer through a MaterialPropertyBlock (_OutlineEnabled, _OutlineColor),
// so one shared material serves every outlined object. Sprites need a transparent margin (Full Rect mesh)
// at least as wide as the outline, or the outline is clipped at the sprite's edge.
Shader "TopDown/Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [PerRendererData] _OutlineEnabled ("Outline Enabled", Float) = 0
        [PerRendererData] _OutlineColor ("Outline Colour", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness (Texels)", Float) = 1
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
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _OutlineEnabled;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            struct VertexInput
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            VertexOutput Vert(VertexInput input)
            {
                VertexOutput output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            // Highest alpha among the eight neighbouring texels at the outline distance
            fixed SampleNeighbourAlpha(float2 uv)
            {
                float2 offset = _MainTex_TexelSize.xy * _OutlineThickness;
                fixed alpha = tex2D(_MainTex, uv + float2(offset.x, 0)).a;
                alpha = max(alpha, tex2D(_MainTex, uv - float2(offset.x, 0)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(0, offset.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv - float2(0, offset.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + offset).a);
                alpha = max(alpha, tex2D(_MainTex, uv - offset).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(offset.x, -offset.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(-offset.x, offset.y)).a);
                return alpha;
            }

            fixed4 Frag(VertexOutput input) : SV_Target
            {
                fixed4 colour = tex2D(_MainTex, input.uv) * input.color;
                colour.rgb *= colour.a;

                // Outline fills only the transparent texels bordering the opaque shape
                fixed outlineAlpha = _OutlineEnabled * SampleNeighbourAlpha(input.uv) * (1 - step(0.001, colour.a)) * _OutlineColor.a;
                fixed4 outline = fixed4(_OutlineColor.rgb * outlineAlpha, outlineAlpha);
                return colour + outline;
            }
            ENDCG
        }
    }
}
