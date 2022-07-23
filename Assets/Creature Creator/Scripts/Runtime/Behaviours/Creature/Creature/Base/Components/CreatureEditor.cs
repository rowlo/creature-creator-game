﻿// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    [RequireComponent(typeof(CreatureCamera))]
    [RequireComponent(typeof(CreatureConstructor), typeof(CreatureMover))]
    public class CreatureEditor : MonoBehaviour
    {
        #region Fields
        [Header("Setup")]
        [SerializeField] private GameObject boneToolPrefab;
        [SerializeField] private GameObject stretchToolPrefab;
        [SerializeField] private GameObject transformationToolsPrefab;
        [SerializeField] private GameObject poofPrefab;
        [Space]
        [SerializeField] private AudioClip stretchAudioClip;
        [SerializeField] private AudioClip resizeAudioClip;

        [Header("Settings")]
        [SerializeField] private float addOrRemoveCooldown = 0.05f;
        [SerializeField] private int angleLimit = 30;

        private MeshCollider meshCollider;
        private Mesh colliderMesh;
        private AudioSource toolsAudioSource;
        private Transform frontTool, backTool;
        private Select select;
        private Drag drag;

        private float addedOrRemovedTime;
        private bool isInteractable, isDirty, isEditing;
        private string loadedCreature;
        private int cash;
        private Vector3? pointerPosOffset;

        private Outline tempModelOutline;
        private GameObject tempModel;

        private BodyPartEditor paintedBodyPart;
        #endregion

        #region Properties
        public float AddOrRemoveCooldown => addOrRemoveCooldown;
        public AudioClip StretchAudioClip => stretchAudioClip;
        public AudioClip ResizeAudioClip => resizeAudioClip;
        public GameObject PoofPrefab => poofPrefab;
        public float AngleLimit => angleLimit;

        public CreatureConstructor CreatureConstructor { get; private set; }
        public CreatureMover CreatureMover { get; private set; }
        public CreatureCamera CreatureCamera { get; private set; }

        public TransformationTools TransformationTools { get; private set; }

        public BodyPartEditor PaintedBodyPart
        {
            get => paintedBodyPart;
            set
            {
                paintedBodyPart = value;

                Color primaryColour = default, sColour = default;
                bool isPrimaryOverridden = false, isSOverride = false;
                if (paintedBodyPart)
                {
                    BodyPartConstructor bpc = paintedBodyPart.BodyPartConstructor;

                    primaryColour = bpc.AttachedBodyPart.primaryColour;
                    if (bpc.IsPrimaryOverridden)
                    {
                        isPrimaryOverridden = true;
                    }
                    else if (bpc.CanOverridePrimary)
                    {
                        primaryColour = CreatureConstructor.Data.PrimaryColour;
                    }

                    sColour = bpc.AttachedBodyPart.secondaryColour;
                    if (bpc.IsSecondaryOverridden)
                    {
                        isSOverride = true;
                    }
                    else if (bpc.CanOverrideSecondary)
                    {
                        sColour = CreatureConstructor.Data.SecondaryColour;
                    }
                }
                else
                {
                    primaryColour = CreatureConstructor.Data.PrimaryColour;
                    sColour = CreatureConstructor.Data.SecondaryColour;
                }
                EditorManager.Instance.SetPrimaryColourUI(primaryColour, isPrimaryOverridden);
                EditorManager.Instance.SetSecondaryColourUI(sColour, isSOverride);

                if (UseTemporaryOutline) tempModelOutline.enabled = !paintedBodyPart;
            }
        }
        public string LoadedCreature
        {
            get => loadedCreature;
            set
            {
                loadedCreature = value;
                EditorManager.Instance.UpdateCreaturesFormatting();
            }
        }
        public int Cash
        {
            get => cash;
            set
            {
                cash = value;
                EditorManager.Instance.UpdateStatistics();
            }
        }

        public bool IsDirty
        {
            get => isDirty;
            set
            {
                if (isDirty != value) // Optimized to only update list when not equal.
                {
                    isDirty = value;
                    EditorManager.Instance.UpdateCreaturesFormatting();
                }
            }
        }
        public bool IsInteractable
        {
            get => isInteractable;
            set
            {
                isInteractable = value;

                foreach (Collider collider in CreatureConstructor.Body.GetComponentsInChildren<Collider>(true))
                {
                    collider.enabled = isInteractable;
                }
            }
        }
        public bool IsEditing
        {
            get => isEditing;
            set
            {
                isEditing = value;

                foreach (BodyPartEditor bpe in GetComponentsInChildren<BodyPartEditor>(true))
                {
                    bpe.enabled = isEditing;
                }
            }
        }
        public bool IsSelected
        {
            get => select.IsSelected;
            set => select.IsSelected = value;
        }
        public bool IsDraggable
        {
            get => drag.draggable;
            set => drag.draggable = value;
        }
        public bool UseTemporaryOutline
        {
            get => tempModel != null;
            set
            {
                if (value)
                {
                    tempModel = new GameObject("Temp");
                    tempModel.transform.SetParent(CreatureConstructor.Model, false);

                    MeshFilter tempBodyMeshFilter = tempModel.AddComponent<MeshFilter>();
                    tempBodyMeshFilter.mesh = colliderMesh;
                    MeshRenderer tempBodyMeshRenderer = tempModel.AddComponent<MeshRenderer>();
                    tempBodyMeshRenderer.materials = new Material[0];

                    tempModelOutline = tempModel.AddComponent<Outline>();
                    tempModelOutline.OutlineWidth = select.Outline.OutlineWidth;
                    tempModelOutline.enabled = select.UseOutline = false;
                }
                else
                {
                    if (tempModel != null)
                    {
                        Destroy(tempModel);
                    }
                    select.UseOutline = true;
                }
            }
        }
        #endregion

        #region Methods
        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            CreatureConstructor = GetComponent<CreatureConstructor>();
            CreatureMover = GetComponent<CreatureMover>();
            CreatureCamera = GetComponent<CreatureCamera>();
        }

        public void Setup()
        {
            SetupInteraction();
            SetupConstruction();
        }
        private void SetupInteraction()
        {
            meshCollider = CreatureConstructor.Model.GetComponent<MeshCollider>();
            colliderMesh = new Mesh(); // Separate mesh used to contain a snapshot of the body for the collider.

            // Interact
            toolsAudioSource = gameObject.AddComponent<AudioSource>();
            toolsAudioSource.volume = 0.25f;

            Hover hover = CreatureConstructor.Body.GetComponent<Hover>();
            hover.OnEnter.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding && !Input.GetMouseButton(0))
                {
                    CreatureCamera.CameraOrbit.Freeze();
                    SetBonesVisibility(true);
                }
            });
            hover.OnExit.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding && !Input.GetMouseButton(0))
                {
                    CreatureCamera.CameraOrbit.Unfreeze();

                    if (!IsSelected)
                    {
                        SetBonesVisibility(false);
                    }
                }
            });

            drag = CreatureConstructor.Body.GetComponent<Drag>();
            drag.OnPress.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    CreatureCamera.CameraOrbit.Freeze();
                }
            });
            drag.OnRelease.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    if (!hover.IsOver)
                    {
                        CreatureCamera.CameraOrbit.Unfreeze();
                    }

                    CreatureConstructor.UpdateDimensions();
                    CreatureConstructor.UpdateConfiguration();
                    foreach (LimbEditor limb in CreatureConstructor.Root.GetComponentsInChildren<LimbEditor>())
                    {
                        limb.UpdateMeshCollider();
                    }

                    EditorManager.Instance.UpdateStatistics();
                    IsDirty = true;
                }
            });
            drag.cylinderRadius = CreatureConstructor.MaxRadius;
            drag.cylinderHeight = CreatureConstructor.MaxHeight;

            CreatureConstructor.Body.GetComponent<Click>();
            select = CreatureConstructor.Body.GetComponent<Select>();
            select.OnSelect.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    SetBonesVisibility(IsSelected);
                    SetArrowsVisibility(IsSelected);
                }
                else
                if (EditorManager.Instance.IsPainting)
                {
                    PaintedBodyPart = null;
                }
            });

            // Tools
            TransformationTools = Instantiate(transformationToolsPrefab, Dynamic.Transform).GetComponent<TransformationTools>();

            frontTool = Instantiate(stretchToolPrefab).transform;
            Press frontToolPress = frontTool.GetComponentInChildren<Press>();
            frontToolPress.OnPress.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    CreatureCamera.CameraOrbit.Freeze();
                    pointerPosOffset = null;
                }
            });
            frontToolPress.OnRelease.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    CreatureCamera.CameraOrbit.Unfreeze();
                }
            });
            frontToolPress.OnHold.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    HandleStretchTools(0, 0, frontTool);
                }
            });

            backTool = Instantiate(stretchToolPrefab).transform;
            Press backToolPress = backTool.GetComponentInChildren<Press>();
            backToolPress.OnPress.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    CreatureCamera.CameraOrbit.Freeze();
                    pointerPosOffset = null;
                }
            });
            backToolPress.OnRelease.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    CreatureCamera.CameraOrbit.Unfreeze();
                }
            });
            backToolPress.OnHold.AddListener(delegate
            {
                if (EditorManager.Instance.IsBuilding)
                {
                    HandleStretchTools(CreatureConstructor.Bones.Count - 1, CreatureConstructor.Bones.Count, backTool);
                }
            });
            
            frontTool.GetChild(0).localPosition = backTool.GetChild(0).localPosition = Vector3.forward * (CreatureConstructor.BoneSettings.Length / 2f + CreatureConstructor.BoneSettings.Radius + 0.05f);
        }
        private void SetupConstruction()
        {
            CreatureConstructor.OnPreDemolish += delegate ()
            {
                // Prevent tools from being demolished along with creature.
                frontTool.parent = backTool.parent = Dynamic.Transform;
                TransformationTools.Hide();
            };
            CreatureConstructor.OnConstructBody += delegate ()
            {
                EditorManager.Instance.UpdateStatistics();
                UpdateMeshCollider();
                CreatureConstructor.IsTextured = CreatureConstructor.IsTextured;
            };
            CreatureConstructor.OnSetupBone += delegate (int index)
            {
                // Hinge Joint
                HingeJoint hingeJoint = CreatureConstructor.Bones[index].GetComponent<HingeJoint>();
                if (hingeJoint != null)
                {
                    hingeJoint.anchor = new Vector3(0, 0, -CreatureConstructor.BoneSettings.Length / 2f);
                    hingeJoint.connectedBody = CreatureConstructor.Bones[index - 1].GetComponent<Rigidbody>();
                }

                // Scroll
                Scroll scroll = CreatureConstructor.Bones[index].GetComponent<Scroll>();
                scroll.OnScrollDown.RemoveAllListeners();
                scroll.OnScrollDown.AddListener(delegate 
                {
                    if (EditorManager.Instance.IsBuilding)
                    {
                        if (CreatureConstructor.GetWeight(index) > 0)
                        {
                            toolsAudioSource.PlayOneShot(ResizeAudioClip);
                        }
                        CreatureConstructor.RemoveWeight(index, 5);

                        UpdateMeshCollider();
                        UpdateBodyPartsAlignment(-1);
                    }
                });
                scroll.OnScrollUp.RemoveAllListeners();
                scroll.OnScrollUp.AddListener(delegate 
                {
                    if (EditorManager.Instance.IsBuilding)
                    {
                        if (CreatureConstructor.GetWeight(index) < 100)
                        {
                            toolsAudioSource.PlayOneShot(ResizeAudioClip);
                        }
                        CreatureConstructor.AddWeight(index, 5);

                        UpdateMeshCollider();
                        UpdateBodyPartsAlignment(1);
                    }
                });
            };
            CreatureConstructor.OnAddBone += delegate (int index)
            {
                // Bone
                GameObject addedBoneGO = CreatureConstructor.Bones[CreatureConstructor.Bones.Count - 1].gameObject;
                Instantiate(boneToolPrefab, addedBoneGO.transform);

                Rigidbody rb = addedBoneGO.AddComponent<Rigidbody>();
                rb.mass = 25f;
                rb.drag = Mathf.Infinity;
                rb.angularDrag = Mathf.Infinity;
                rb.useGravity = false;

                if (CreatureConstructor.Data.Bones.Count > 0) // Necessary to use bone data instead of childCount.
                {
                    HingeJoint hingeJoint = addedBoneGO.AddComponent<HingeJoint>();

                    hingeJoint.useLimits = true;
                    hingeJoint.limits = new JointLimits()
                    {
                        min = -angleLimit,
                        max = angleLimit,
                        bounceMinVelocity = 0
                    };
                    hingeJoint.enablePreprocessing = false;
                }

                // Tools
                addedBoneGO.AddComponent<Scroll>();

                Hover hover = addedBoneGO.AddComponent<Hover>();
                hover.OnEnter.AddListener(delegate
                {
                    if (EditorManager.Instance.IsBuilding && !Input.GetMouseButton(0))
                    {
                        CreatureCamera.CameraOrbit.Freeze();
                        SetBonesVisibility(true);
                    }
                });
                hover.OnExit.AddListener(delegate
                {
                    if (EditorManager.Instance.IsBuilding && !Input.GetMouseButton(0))
                    {
                        CreatureCamera.CameraOrbit.Unfreeze();

                        if (!IsSelected)
                        {
                            SetBonesVisibility(false);
                        }
                    }
                });

                Drag drag = addedBoneGO.AddComponent<Drag>();
                drag.OnPress.AddListener(delegate
                {
                    if (EditorManager.Instance.IsBuilding)
                    {
                        foreach (Transform bone in CreatureConstructor.Bones)
                        {
                            bone.GetComponentInChildren<Collider>().isTrigger = true;
                        }

                        CreatureCamera.CameraOrbit.Freeze();
                    }
                });
                drag.OnRelease.AddListener(delegate
                {
                    if (EditorManager.Instance.IsBuilding)
                    {
                        foreach (Transform bone in CreatureConstructor.Bones)
                        {
                            bone.GetComponentInChildren<Collider>().isTrigger = false;
                        }

                        if (!hover.IsOver)
                        {
                            CreatureCamera.CameraOrbit.Unfreeze();
                        }

                        CreatureConstructor.UpdateOrigin();
                        CreatureConstructor.UpdateConfiguration();
                        CreatureConstructor.UpdateDimensions();

                        UpdateMeshCollider();
                        foreach (LimbEditor limb in CreatureConstructor.Root.GetComponentsInChildren<LimbEditor>(true))
                        {
                            limb.UpdateMeshCollider();
                        }

                        EditorManager.Instance.UpdateStatistics();
                        IsDirty = true;
                    }
                });
                drag.mousePlaneAlignment = Drag.MousePlaneAlignment.ToWorldDirection;
                drag.world = transform;
                drag.boundsShape = Drag.BoundsShape.Cylinder;
                drag.cylinderHeight = CreatureConstructor.MaxHeight;
                drag.cylinderRadius = CreatureConstructor.MaxRadius;
                drag.updatePlaneOnPress = true;

                Click click = addedBoneGO.AddComponent<Click>();
                click.OnClick.AddListener(delegate
                {
                    if (EditorManager.Instance.IsBuilding)
                    {
                        IsSelected = true;
                    }
                });

                frontTool.SetParent(CreatureConstructor.Bones[0]);
                backTool.SetParent(CreatureConstructor.Bones[CreatureConstructor.Bones.Count - 1]);

                frontTool.localPosition = backTool.localPosition = Vector3.zero;
                frontTool.localRotation = Quaternion.Euler(0, 180, 0);
                backTool.localRotation = Quaternion.identity;

                // Editor
                IsDirty = true;
            };
            CreatureConstructor.OnPreRemoveBone += delegate (int index)
            {
                backTool.SetParent(CreatureConstructor.Bones[CreatureConstructor.Bones.Count - 2]);
                backTool.localPosition = frontTool.localPosition = Vector3.zero;
                backTool.localRotation = Quaternion.identity;
            };
            CreatureConstructor.OnRemoveBone += delegate (int index)
            {
                IsDirty = true;
            };
            CreatureConstructor.OnSetWeight += delegate (int index, float weight)
            {
                //UpdateMeshCollider(); // Causes a considerable amount of lag when constructing.

                Transform bone = CreatureConstructor.Bones[index];

                // Bone model
                Transform model = bone.GetChild(0);
                float x = Mathf.Lerp(0.5f, 1.5f, weight / 100f);
                float y = Mathf.Lerp(0.5f, 1.5f, weight / 100f);
                float z = 1f;
                model.localScale = new Vector3(x, y, z);

                EditorManager.Instance.UpdateStatistics();
                IsDirty = true;
            };
            CreatureConstructor.OnSetPrimaryColour += delegate (Color colour)
            {
                IsDirty = true;
            };
            CreatureConstructor.OnSetSecondaryColour += delegate (Color colour)
            {
                IsDirty = true;
            };
            CreatureConstructor.OnSetPattern += delegate (string patternID)
            {
                IsDirty = true;
            };
            CreatureConstructor.OnSetTiling += delegate (Vector2 tiling)
            {
                IsDirty = true;
                EditorManager.Instance.SetTilingUI(tiling);
            };
            CreatureConstructor.OnSetOffset += delegate (Vector2 offset)
            {
                IsDirty = true;
                EditorManager.Instance.SetOffsetUI(offset);
            };
            CreatureConstructor.OnSetShine += delegate (float shine)
            {
                IsDirty = true;
                EditorManager.Instance.SetShineUI(shine);
            };
            CreatureConstructor.OnSetMetallic += delegate (float metallic)
            {
                IsDirty = true;
                EditorManager.Instance.SetMetallicUI(metallic);
            };
            CreatureConstructor.OnAddBodyPartPrefab += delegate (GameObject main, GameObject flipped)
            {
                BodyPartEditor mainBPE = main.GetComponent<BodyPartEditor>();
                mainBPE.Setup(this);

                BodyPartEditor flippedBPE = flipped.GetComponent<BodyPartEditor>();
                flippedBPE.SetFlipped(mainBPE);
                flippedBPE.Setup(this);
            };
            CreatureConstructor.OnAddBodyPartData += delegate (BodyPart bodyPart)
            {
                Cash -= bodyPart.Price;
            };
            CreatureConstructor.OnRemoveBodyPartData += delegate (BodyPart bodyPart)
            {
                Cash += bodyPart.Price;
            };
            CreatureConstructor.OnBodyPartPrefabOverride += delegate (BodyPart bodyPart)
            {
                return bodyPart.GetPrefab(BodyPart.PrefabType.Editable);
            };
            CreatureConstructor.OnConstructCreature += delegate
            {
                if (EditorManager.Instance.IsPainting)
                {
                    UseTemporaryOutline = false;
                    UseTemporaryOutline = true;
                }
            };
        }
        
        public void Load(CreatureData creatureData)
        {
            CreatureConstructor.Demolish();

            Cash = ProgressManager.Data.Cash;
            if (creatureData != null)
            {
                CreatureConstructor.Construct(creatureData);

                LoadedCreature = creatureData.Name;
            }
            else
            {
                CreatureConstructor.AddBone(0, Vector3.up * 1.5f, Quaternion.identity, 0f);
                CreatureConstructor.AddBoneToBack();
                CreatureConstructor.Body.localPosition = new Vector3(0, CreatureConstructor.Body.localPosition.y, 0); // Added bones aren't initially center-aligned.
                CreatureConstructor.SetPrimaryColour(Color.white);
                CreatureConstructor.SetSecondaryColour(Color.black);
                CreatureConstructor.SetPattern("");
                CreatureConstructor.SetTiling(Vector2.one);
                CreatureConstructor.SetOffset(Vector2.zero);
                CreatureConstructor.SetShine(0f);
                CreatureConstructor.SetMetallic(0f);

                LoadedCreature = null;
            }

            CreatureConstructor.IsTextured = CreatureConstructor.IsTextured;
            IsInteractable = IsInteractable;
            IsDirty = false;

            Deselect();
        }

        public void Deselect()
        {
            select.Outline.enabled = false;
            PaintedBodyPart = null;

            SetBonesVisibility(false);
            SetArrowsVisibility(false);

            foreach (BodyPartEditor bpe in GetComponentsInChildren<BodyPartEditor>())
            {
                bpe.Deselect();
            }
        }

        public void SetBonesVisibility(bool isVisible)
        {
            foreach (Transform bone in CreatureConstructor.Bones)
            {
                bone.GetChild(0).gameObject.SetActive(isVisible);
            }
        }
        public void SetArrowsVisibility(bool isVisible)
        {
            backTool.gameObject.SetActive(isVisible);
            frontTool.gameObject.SetActive(isVisible);
        }

        public void UpdateMeshCollider()
        {
            colliderMesh.Clear();
            CreatureConstructor.SkinnedMeshRenderer.BakeMesh(colliderMesh);
            meshCollider.sharedMesh = colliderMesh;
        }
        public void UpdateBodyPartsAlignment(int alignment)
        {
            foreach (Transform bone in CreatureConstructor.Bones)
            {
                foreach (BodyPartConstructor bpc in bone.GetComponentsInChildren<BodyPartConstructor>())
                {
                    Vector3 currentDir = (bpc.transform.position - bone.position).normalized;
                    Vector3 alignmentDir = alignment * currentDir;

                    if (!Physics.Raycast(bpc.transform.position + currentDir, -currentDir, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("Body")))
                    {
                        continue;
                    }

                    Vector3 displacement = hitInfo.point - bpc.transform.position;
                    if (Vector3.Dot(displacement, alignmentDir) > 0)
                    {
                        bpc.transform.position = hitInfo.point;
                    }
                }
            }
        }

        private void HandleStretchTools(int oldIndex, int newIndex, Transform tool)
        {
            Plane stretchPlane = new Plane(transform.right, CreatureMover.Platform.transform.position);

            Ray ray = CreatureCamera.Camera.ScreenPointToRay(Input.mousePosition);
            if (stretchPlane.Raycast(ray, out float distance))
            {
                Vector3 pointerPos = ray.GetPoint(distance);

                if (pointerPosOffset == null)
                {
                    pointerPosOffset = tool.InverseTransformPoint(pointerPos);
                }

                Vector3 initialPointerPos = tool.TransformPoint((Vector3)pointerPosOffset);
                bool isFarEnough = Vector3Utility.SqrDistanceComp(pointerPos, initialPointerPos, CreatureConstructor.BoneSettings.Length);
                bool hasCooledDown = Time.time > addedOrRemovedTime + AddOrRemoveCooldown;

                if (isFarEnough && hasCooledDown)
                {
                    Vector3 pointerDisplacement = pointerPos - initialPointerPos;
                    Vector3 toolDisplacement = pointerPos - tool.position;

                    float pointerAngle = Vector3.Angle(pointerDisplacement, tool.forward);
                    float toolAngle = Vector3.Angle(toolDisplacement, tool.forward);

                    if ((pointerAngle < 90f) && (toolAngle < angleLimit) && CreatureConstructor.CanAddBone(oldIndex))
                    {
                        CreatureConstructor.UpdateBoneConfiguration(); // Necessary in-case creature moves slightly while editing (due to hinge-joint bounciness).

                        Vector3 position = CreatureConstructor.Bones[oldIndex].position + ((0.5f * CreatureConstructor.BoneSettings.Length) * (tool.forward + toolDisplacement.normalized));
                        Quaternion rotation = Quaternion.LookRotation(Mathf.Sign(Vector3.Dot(tool.forward, CreatureConstructor.Bones[oldIndex].forward)) * toolDisplacement.normalized, tool.up);

                        position = transform.InverseTransformPoint(position);
                        rotation = Quaternion.Inverse(transform.rotation) * rotation;

                        CreatureConstructor.AddBone(newIndex, position, rotation, Mathf.Clamp(CreatureConstructor.GetWeight(oldIndex) * 0.75f, 0f, 100f));
                    }
                    else if (pointerAngle > (180f - angleLimit) && CreatureConstructor.CanRemoveBone(oldIndex))
                    {
                        CreatureConstructor.RemoveBone(oldIndex);
                    }
                    else
                    {
                        return; // Prevent editor from providing feedback if no bone was added/removed.
                    }

                    toolsAudioSource.PlayOneShot(StretchAudioClip);
                    addedOrRemovedTime = Time.time;
                }
            }
        }
        #endregion
    }
}