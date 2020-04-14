using System;
using ProtoBuf;
using UnityEngine;
using UWE;
using Straitjacket.Harmony;

namespace NewSubnautica
{
	// Token: 0x0200013C RID: 316
	[ProtoContract]
	[RequireComponent(typeof(SwimBehaviour))]
	public class CollectShinyV2 : CreatureAction, IProtoTreeEventListener, IManagedLateUpdateBehaviour, IManagedBehaviour
	{
		float prio;

		public string GetProfileTag()
		{
			return "CollectShiny";
		}

		public int managedLateUpdateIndex { get; set; }

		private bool IsTargetValid(IEcoTarget target)
		{
			return target.GetGameObject().GetComponentInParent<Player>() == null && (target.GetPosition() - this.creature.leashPosition).sqrMagnitude > 64f;
		}

		private void Start()
		{
			this.isTargetValidFilter = new EcoRegion.TargetFilter(this.IsTargetValid);
		}

		private void UpdateShinyTarget()
		{
			GameObject gameObject = null;
			if (EcoRegionManager.main != null)
			{
				IEcoTarget ecoTarget = EcoRegionManager.main.FindNearestTarget(EcoTargetType.Shiny, base.transform.position, this.isTargetValidFilter, 1);
				if (ecoTarget != null)
				{
					gameObject = ecoTarget.GetGameObject();
					//Debug.DrawLine(base.transform.position, ecoTarget.GetPosition(), Color.red, 2f);
				}
				else
				{
					gameObject = null;
				}
			}
			if (gameObject)
			{
				Vector3 direction = gameObject.transform.position - base.transform.position;
				float num = direction.magnitude - 0.5f;
				if (num > 0f && Physics.Raycast(base.transform.position, direction, num, Voxeland.GetTerrainLayerMask()))
				{
					gameObject = null;
				}
			}
			if (this.shinyTarget != gameObject && gameObject != null && gameObject.GetComponent<Rigidbody>() != null && gameObject.GetComponent<Pickupable>() != null)
			{
				if (this.shinyTarget != null)
				{
					if ((gameObject.transform.position - base.transform.position).magnitude > (this.shinyTarget.transform.position - base.transform.position).magnitude)
					{
						this.DropShinyTarget();
						this.shinyTarget = gameObject;
						return;
					}
				}
				else
				{
					this.shinyTarget = gameObject;
				}
			}
		}

		public override float Evaluate(Creature creature)
		{
			if (this.timeNextFindShiny < Time.time)
			{
				this.UpdateShinyTarget();
				this.timeNextFindShiny = Time.time + this.updateTargetInterval * (1f + 0.2f * UnityEngine.Random.value);
			}

			if (this.shinyTarget != null && this.shinyTarget.activeInHierarchy)
			{
				prio = base.GetEvaluatePriority();

				return prio;
			}
			return 0f;
		}

		public override void StopPerform(Creature creature)
		{
			this.DropShinyTarget();
		}

		private void TryPickupShinyTarget()
		{
			if (this.shinyTarget != null && this.shinyTarget.activeInHierarchy)
			{
				base.SendMessage("OnShinyPickUp", this.shinyTarget, SendMessageOptions.DontRequireReceiver);
				this.shinyTarget.gameObject.SendMessage("OnShinyPickUp", base.gameObject, SendMessageOptions.DontRequireReceiver);
				UWE.Utils.SetCollidersEnabled(this.shinyTarget, false);
				this.shinyTarget.transform.parent = this.shinyTargetAttach;
				this.shinyTarget.transform.localPosition = Vector3.zero;
				this.targetPickedUp = true;
				UWE.Utils.SetIsKinematic(this.shinyTarget.GetComponent<Rigidbody>(), true);
				UWE.Utils.SetEnabled(this.shinyTarget.GetComponent<LargeWorldEntity>(), false);
				base.SendMessage("OnShinyPickedUp", this.shinyTarget, SendMessageOptions.DontRequireReceiver);
				base.swimBehaviour.SwimTo(base.transform.position + Vector3.up * 5f + UnityEngine.Random.onUnitSphere, Vector3.up, this.swimVelocity);
				this.timeNextSwim = Time.time + 1f;
				BehaviourUpdateUtils.Register(this);
			}
		}

		private void DropShinyTarget()
		{
			if (this.shinyTarget != null && this.targetPickedUp)
			{
				this.DropShinyTarget(this.shinyTarget);
				this.shinyTarget = null;
				this.targetPickedUp = false;
				BehaviourUpdateUtils.Deregister(this);
			}
		}

		private void DropShinyTarget(GameObject target)
		{
			target.transform.parent = null;
			UWE.Utils.SetCollidersEnabled(target, true);
			UWE.Utils.SetIsKinematic(target.GetComponent<Rigidbody>(), false);
			LargeWorldEntity component = target.GetComponent<LargeWorldEntity>();
			if (component && LargeWorldStreamer.main)
			{
				LargeWorldStreamer.main.cellManager.RegisterEntity(component);
			}
			target.gameObject.SendMessage("OnShinyDropped", base.gameObject, SendMessageOptions.DontRequireReceiver);
		}

		private bool CloseToShinyTarget()
		{
			return (base.transform.position - this.shinyTarget.transform.position).sqrMagnitude < 16f;
		}

		private bool CloseToNest()
		{
			return (base.transform.position - this.creature.leashPosition).sqrMagnitude < 16f;
		}

		public override void Perform(Creature creature, float deltaTime)
		{
			//Debugger.Log("shiny prio : " + prio);

			if (this.shinyTarget != null)
			{
				if (!this.targetPickedUp)
				{
					if (Time.time > this.timeNextSwim)
					{
						this.timeNextSwim = Time.time + this.swimInterval;
						base.swimBehaviour.SwimTo(this.shinyTarget.transform.position, -Vector3.up, this.swimVelocity);
					}

					if (this.CloseToShinyTarget())
					{
						this.TryPickupShinyTarget();
						return;
					}
				}
				else
				{
					if (this.shinyTarget.transform.parent != this.shinyTargetAttach)
					{
						if (this.shinyTarget.transform.parent != null && this.shinyTarget.transform.parent.GetComponentInParent<Stalker>() != null)
						{
							this.targetPickedUp = false;
							this.shinyTarget = null;
						}
						else
						{
							this.TryPickupShinyTarget();
						}
					}
					if (Time.time > this.timeNextSwim)
					{
						this.timeNextSwim = Time.time + this.swimInterval;
						base.swimBehaviour.SwimTo(creature.leashPosition + new Vector3(0f, 2f, 0f), this.swimVelocity);
					}
					if (this.CloseToNest())
					{
						this.DropShinyTarget();
						creature.Happy.Add(1f);
					}
				}
			}
		}

		public void ManagedLateUpdate()
		{
			if (this.shinyTarget != null && this.targetPickedUp)
			{
				this.shinyTargetAttach.position = this.mouth.position;
				return;
			}
			BehaviourUpdateUtils.Deregister(this);
		}

		private void OnDisable()
		{
			this.DropShinyTarget();
		}

		private void OnDestroy()
		{
			BehaviourUpdateUtils.Deregister(this);
		}

		public void OnProtoSerializeObjectTree(ProtobufSerializer serializer)
		{
		}

		public void OnProtoDeserializeObjectTree(ProtobufSerializer serializer)
		{
			foreach (object obj in this.shinyTargetAttach)
			{
				Transform transform = (Transform)obj;
				this.DropShinyTarget(transform.gameObject);
			}
		}

		[AssertNotNull]
		public Transform mouth;

		[AssertNotNull]
		public Transform shinyTargetAttach;

		public string eventQualifier = "";

		private GameObject shinyTarget;

		private bool targetPickedUp;

		private float timeNextFindShiny;

		public float swimVelocity = 3f;

		public float swimInterval = 5f;

		public float updateTargetInterval = 1f;

		private float timeNextSwim;

		private EcoRegion.TargetFilter isTargetValidFilter;
	}
}
