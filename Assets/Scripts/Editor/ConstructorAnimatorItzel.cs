using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Genera Assets/Animations/Itzel/Itzel.controller desde código.
///
///                  ┌──────────── Any State ── Saltar (trigger) ───────────┐
///                  ▼                                                        │
///  ┌─────────────────────┐  EnSuelo = false  ┌─────────┐  fin del clip  ┌───┴─────┐
///  │ Locomoción (Blend)  │ ────────────────► │ Cayendo │ ◄───────────── │  Salto  │
///  │ idle→caminar→correr │                   └────┬────┘                └─────────┘
///  └──┬───────────────▲──┘                        │ EnSuelo = true
///     │ Agachada      │ fin del clip         ┌────▼───────┐
///     ▼               └───────────────────── │ Aterrizaje │
///  ┌──────────────────┐                      └────────────┘
///  │ Agachada (Blend) │ ── Agachada = false ──► Locomoción
///  └──────────────────┘
/// </summary>
public static class ConstructorAnimatorItzel
{
    public const string rutaController = "Assets/Animations/Itzel/Itzel.controller";
    private const string rutaAnimaciones = "Assets/Models/Itzel/Animaciones/UAL1_Standard.fbx";

    // Velocidades del GDD 5.2 (m/s), usadas como umbrales del Blend Tree.
    private const float velocidadCaminar = 3.5f;
    private const float velocidadCorrer = 6.5f;
    private const float velocidadAgachada = velocidadCaminar * 0.55f;

    // Las animaciones están hechas para un adulto; Itzel es más pequeña y se mueve más rápido,
    // así que aceleramos los ciclos para que los pies no "patinen".
    private const float escalaTiempoCaminar = 1.6f;
    private const float escalaTiempoCorrer = 1.3f;
    private const float escalaTiempoAgachada = 1.4f;

    private const float duracionTransicion = 0.1f;

    [MenuItem("Lamento/Construir Animator de Itzel", priority = 10)]
    public static void Construir()
    {
        AnimationClip idle = BuscarClip("Idle_Loop");
        AnimationClip caminar = BuscarClip("Walk_Loop");
        AnimationClip correr = BuscarClip("Jog_Fwd_Loop");
        AnimationClip inicioSalto = BuscarClip("Jump_Start");
        AnimationClip caida = BuscarClip("Jump_Loop");
        AnimationClip aterrizaje = BuscarClip("Jump_Land");
        AnimationClip agachadaQuieta = BuscarClip("Crouch_Idle_Loop");
        AnimationClip agachadaAndando = BuscarClip("Crouch_Fwd_Loop");

        if (new[] { idle, caminar, correr, inicioSalto, caida, aterrizaje, agachadaQuieta, agachadaAndando }.Any(c => c == null))
        {
            Debug.LogError("[Lamento] Faltan clips en UAL1_Standard.fbx. Ejecuta primero 'Lamento > Reimportar modelos de Itzel'.");
            return;
        }

        AnimatorController controller = ObtenerControllerLimpio();

        controller.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        controller.AddParameter("EnSuelo", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Agachada", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VelocidadVertical", AnimatorControllerParameterType.Float);
        controller.AddParameter("Saltar", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine maquina = controller.layers[0].stateMachine;

        // ── Locomoción: Blend Tree 1D según Velocidad ──
        AnimatorState locomocion = controller.CreateBlendTreeInController("Locomocion", out BlendTree arbolLocomocion, 0);
        ConfigurarArbol(arbolLocomocion);
        arbolLocomocion.AddChild(idle, 0f);
        arbolLocomocion.AddChild(caminar, velocidadCaminar);
        arbolLocomocion.AddChild(correr, velocidadCorrer);
        AplicarEscalasDeTiempo(arbolLocomocion, 1f, escalaTiempoCaminar, escalaTiempoCorrer);
        maquina.defaultState = locomocion;

        // ── Agachada: Blend Tree 1D (quieta → avanzando) ──
        AnimatorState agachada = controller.CreateBlendTreeInController("Agachada", out BlendTree arbolAgachada, 0);
        ConfigurarArbol(arbolAgachada);
        arbolAgachada.AddChild(agachadaQuieta, 0f);
        arbolAgachada.AddChild(agachadaAndando, velocidadAgachada);
        AplicarEscalasDeTiempo(arbolAgachada, 1f, escalaTiempoAgachada);

        // ── Salto, caída y aterrizaje ──
        AnimatorState salto = maquina.AddState("Salto");
        salto.motion = inicioSalto;
        AnimatorState cayendo = maquina.AddState("Cayendo");
        cayendo.motion = caida;
        AnimatorState aterrizando = maquina.AddState("Aterrizaje");
        aterrizando.motion = aterrizaje;

        // ── Transiciones ──
        AnimatorStateTransition alSaltar = maquina.AddAnyStateTransition(salto);
        alSaltar.AddCondition(AnimatorConditionMode.If, 0f, "Saltar");
        alSaltar.canTransitionToSelf = false;
        ConfigurarTransicion(alSaltar, false, 0f);

        AnimatorStateTransition saltoACaida = salto.AddTransition(cayendo);
        ConfigurarTransicion(saltoACaida, true, 0.85f);

        AnimatorStateTransition saltoAAterrizaje = salto.AddTransition(aterrizando);
        saltoAAterrizaje.AddCondition(AnimatorConditionMode.If, 0f, "EnSuelo");
        saltoAAterrizaje.AddCondition(AnimatorConditionMode.Less, 0.01f, "VelocidadVertical");
        ConfigurarTransicion(saltoAAterrizaje, false, 0f);

        AnimatorStateTransition locomocionACaida = locomocion.AddTransition(cayendo);
        locomocionACaida.AddCondition(AnimatorConditionMode.IfNot, 0f, "EnSuelo");
        locomocionACaida.AddCondition(AnimatorConditionMode.Less, -1f, "VelocidadVertical");
        ConfigurarTransicion(locomocionACaida, false, 0f);

        AnimatorStateTransition caidaAAterrizaje = cayendo.AddTransition(aterrizando);
        caidaAAterrizaje.AddCondition(AnimatorConditionMode.If, 0f, "EnSuelo");
        ConfigurarTransicion(caidaAAterrizaje, false, 0f);

        AnimatorStateTransition aterrizajeALocomocion = aterrizando.AddTransition(locomocion);
        ConfigurarTransicion(aterrizajeALocomocion, true, 0.5f);

        AnimatorStateTransition aterrizajeAAgachada = aterrizando.AddTransition(agachada);
        aterrizajeAAgachada.AddCondition(AnimatorConditionMode.If, 0f, "Agachada");
        ConfigurarTransicion(aterrizajeAAgachada, false, 0f);

        AnimatorStateTransition locomocionAAgachada = locomocion.AddTransition(agachada);
        locomocionAAgachada.AddCondition(AnimatorConditionMode.If, 0f, "Agachada");
        locomocionAAgachada.AddCondition(AnimatorConditionMode.If, 0f, "EnSuelo");
        ConfigurarTransicion(locomocionAAgachada, false, 0f);

        AnimatorStateTransition agachadaALocomocion = agachada.AddTransition(locomocion);
        agachadaALocomocion.AddCondition(AnimatorConditionMode.IfNot, 0f, "Agachada");
        ConfigurarTransicion(agachadaALocomocion, false, 0f);

        AnimatorStateTransition agachadaACaida = agachada.AddTransition(cayendo);
        agachadaACaida.AddCondition(AnimatorConditionMode.IfNot, 0f, "EnSuelo");
        agachadaACaida.AddCondition(AnimatorConditionMode.Less, -1f, "VelocidadVertical");
        ConfigurarTransicion(agachadaACaida, false, 0f);

        // Posiciones en la ventana Animator para que el diagrama se lea bien en clase.
        ColocarEstado(maquina, locomocion, new Vector3(300f, 120f));
        ColocarEstado(maquina, agachada, new Vector3(300f, 280f));
        ColocarEstado(maquina, salto, new Vector3(600f, 0f));
        ColocarEstado(maquina, cayendo, new Vector3(600f, 120f));
        ColocarEstado(maquina, aterrizando, new Vector3(600f, 240f));
        maquina.anyStatePosition = new Vector3(300f, 0f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Lamento] Animator de Itzel construido en {rutaController}.", controller);
        Selection.activeObject = controller;
    }

    private static AnimatorController ObtenerControllerLimpio()
    {
        UtilidadesEditorLamento.AsegurarCarpeta("Assets/Animations/Itzel");
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaController);

        if (controller == null)
        {
            return AnimatorController.CreateAnimatorControllerAtPath(rutaController);
        }

        // Reutilizamos el mismo asset (mismo GUID) para no romper la referencia del prefab.
        while (controller.parameters.Length > 0)
        {
            controller.RemoveParameter(0);
        }

        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        foreach (AnimatorStateTransition transicion in maquina.anyStateTransitions)
        {
            maquina.RemoveAnyStateTransition(transicion);
        }

        foreach (ChildAnimatorState hijo in maquina.states)
        {
            maquina.RemoveState(hijo.state);
        }

        foreach (Object subasset in AssetDatabase.LoadAllAssetsAtPath(rutaController))
        {
            if (subasset is BlendTree)
            {
                AssetDatabase.RemoveObjectFromAsset(subasset);
                Object.DestroyImmediate(subasset, true);
            }
        }

        return controller;
    }

    private static AnimationClip BuscarClip(string nombre)
    {
        return AssetDatabase.LoadAllAssetsAtPath(rutaAnimaciones)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .FirstOrDefault(clip => clip.name == nombre || clip.name.EndsWith("|" + nombre));
    }

    private static void ConfigurarArbol(BlendTree arbol)
    {
        arbol.blendType = BlendTreeType.Simple1D;
        arbol.blendParameter = "Velocidad";
        arbol.useAutomaticThresholds = false;
    }

    private static void AplicarEscalasDeTiempo(BlendTree arbol, params float[] escalas)
    {
        ChildMotion[] hijos = arbol.children;
        for (int i = 0; i < hijos.Length && i < escalas.Length; i++)
        {
            hijos[i].timeScale = escalas[i];
        }

        arbol.children = hijos;
    }

    private static void ConfigurarTransicion(AnimatorStateTransition transicion, bool conTiempoDeSalida, float tiempoDeSalida)
    {
        transicion.hasExitTime = conTiempoDeSalida;
        transicion.exitTime = tiempoDeSalida;
        transicion.hasFixedDuration = true;
        transicion.duration = duracionTransicion;
    }

    private static void ColocarEstado(AnimatorStateMachine maquina, AnimatorState estado, Vector3 posicion)
    {
        ChildAnimatorState[] estados = maquina.states;
        for (int i = 0; i < estados.Length; i++)
        {
            if (estados[i].state == estado)
            {
                estados[i].position = posicion;
            }
        }

        maquina.states = estados;
    }
}
