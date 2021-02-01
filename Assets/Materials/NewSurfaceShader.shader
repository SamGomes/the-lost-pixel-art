// Compiled shader for PC, Mac & Linux Standalone

//////////////////////////////////////////////////////////////////////////
// 
// NOTE: This is *not* a valid shader file, the contents are provided just
// for information and for debugging purposes only.
// 
//////////////////////////////////////////////////////////////////////////
// Skipping shader variants that would not be included into build of current scene.

Shader "Sprites/Diffuse" {
Properties {
[PerRendererData]  _MainTex ("Sprite Texture", 2D) = "white" { }
 _Color ("Tint", Color) = (1.000000,1.000000,1.000000,1.000000)
[MaterialToggle]  PixelSnap ("Pixel snap", Float) = 0.000000
[HideInInspector]  _RendererColor ("RendererColor", Color) = (1.000000,1.000000,1.000000,1.000000)
[HideInInspector]  _Flip ("Flip", Vector) = (1.000000,1.000000,1.000000,1.000000)
[PerRendererData]  _AlphaTex ("External Alpha", 2D) = "white" { }
[PerRendererData]  _EnableExternalAlpha ("Enable External Alpha", Float) = 0.000000
}
SubShader { 
 Tags { "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType"="Plane" }
 Pass {
    Cull Back
    ZTest Always
    ZWrite Off
  Name "FORWARD"
  Tags { "LIGHTMODE"="FORWARDBASE" "QUEUE"="Transparent" "IGNOREPROJECTOR"="true" "SHADOWSUPPORT"="true" "RenderType"="Transparent" "CanUseSpriteAtlas"="true" "PreviewType"="Plane" "Queue"="Overlay+1"}