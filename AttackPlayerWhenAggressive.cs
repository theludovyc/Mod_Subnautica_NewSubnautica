using System;
using UnityEngine;

namespace NewSubnautica
{
	[RequireComponent(typeof(SwimBehaviour))]
	public class AttackPlayerWhenAggressive : CreatureAction
	{
		public override float Evaluate(Creature creature)
		{
			if ((creature.Aggression.Value > this.aggressionThreshold | Time.time < this.timeStartAttack + this.minAttackDuration) & Time.time > this.timeStopAttack + this.pauseInterval)
			{
				var p = Player.main;

				if (p != null && p.CanBeAttacked())
				{
					return base.GetEvaluatePriority();
				}
			}
			return 0f;
		}

		public override void StartPerform(Creature creature)
		{
			this.timeStartAttack = Time.time;
			if (this.attackStartSound)
			{
				this.attackStartSound.Play();
			}
			if (this.attackStartFXcontrol != null)
			{
				this.attackStartFXcontrol.Play();
			}
			SafeAnimator.SetBool(creature.GetAnimator(), "attacking", true);
		}

		public override void StopPerform(Creature creature)
		{
			SafeAnimator.SetBool(creature.GetAnimator(), "attacking", false);
			if (this.attackStartFXcontrol != null)
			{
				this.attackStartFXcontrol.Stop();
			}
			this.timeStopAttack = Time.time;
		}

		public override void Perform(Creature creature, float deltaTime)
		{
			ProfilingUtils.BeginSample("CreatureAction::Perform (AttackEcoTarget)");

			if (Time.time > this.timeNextSwim)
			{
				this.timeNextSwim = Time.time + this.swimInterval;

				Vector3 position = Player.main.transform.position;

				Vector3 targetDirection = -MainCamera.camera.transform.forward;

				base.swimBehaviour.Attack(position, targetDirection, this.swimVelocity);
			}

			if (this.resetAggressionOnTime && Time.time > this.timeStartAttack + this.maxAttackDuration)
			{
				this.StopAttack();
			}

			ProfilingUtils.EndSample(null);
		}

		public void OnMeleeAttack(GameObject target)
		{
			this.StopAttack();
		}

		protected virtual void StopAttack()
		{
			this.creature.Aggression.Value = 0f;
			this.timeStopAttack = Time.time;
			if (this.attackStartFXcontrol != null)
			{
				this.attackStartFXcontrol.Stop();
			}
		}

		public float swimVelocity = 10f;

		public float swimInterval = 0.8f;

		public float aggressionThreshold = 0.75f;

		public float minAttackDuration = 3f;

		public float maxAttackDuration = 7f;

		public float pauseInterval = 20f;

		public float rememberTargetTime = 5f;

		public bool resetAggressionOnTime = true;

		public FMOD_CustomEmitter attackStartSound;

		public VFXController attackStartFXcontrol;

		private float timeStartAttack;

		private float timeStopAttack;

		private float timeNextSwim;
	}
}