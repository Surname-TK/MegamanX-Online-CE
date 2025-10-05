using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BubbleSplash : Weapon {
	public static BubbleSplash netWeapon = new();

	public List<BubbleSplashProj> bubblesOnField = new List<BubbleSplashProj>();
	public List<float> bubbleAfterlifeTimes = new List<float>();
	public float hyperChargeDelay;
	public bool freeAmmoNextCharge;

	public BubbleSplash() : base() {
		displayName = "Bubble Splash";
		shootSounds = new string[] { "bubbleSplash", "bubbleSplash", "bubbleSplash", "bubbleSplashCharged" };
		fireRate = 2;
		isStream = true;
		index = (int)WeaponIds.BubbleSplash;
		weaponBarBaseIndex = 10;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = 10;
		killFeedIndex = 21;
		weaknessIndex = (int)WeaponIds.SpinWheel;
		switchCooldown = 15;
		damage = "1/1*6";
		ammousage = 0.5;
		effect = "U:Bubbles have less gravity underwater.\nC:Grants Jump boost.\nBoth:Will destroy Speed Burner uncharged on contact.";
		maxAmmo = 28;
		ammo = maxAmmo;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 3) {
			if (freeAmmoNextCharge) {
				freeAmmoNextCharge = false;
				return 0;
			}
			return 7;
		}
		return 0.125f;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		if (hyperChargeDelay > 0) return false;

		return bubblesOnField.Count + bubbleAfterlifeTimes.Count < 7;
	}

	public override void update() {
		base.update();
		Helpers.decrementFrames(ref hyperChargeDelay);

		for (int i = bubblesOnField.Count - 1; i >= 0; i--) {
			if (bubblesOnField[i].destroyed) {
				float timeCutShort = bubblesOnField[i].maxTime - bubblesOnField[i].time;
				bubbleAfterlifeTimes.Add(0.6f);
				bubblesOnField.RemoveAt(i);
			continue;
			}
		}

		for (int i = bubbleAfterlifeTimes.Count - 1; i >= 0; i--) {
			bubbleAfterlifeTimes[i] -= Global.spf;
			if (bubbleAfterlifeTimes[i] <= 0) {
				bubbleAfterlifeTimes.RemoveAt(i);
			}
		}
	}

	// Friendly reminder that this method MUST be deterministic across all clients,
	// i.e. don't vary it on a field that could vary locally.
	// Gacel:
	// Uh. Well. Not really. This does not run by local players.
	// Just make sure you send all the extra data in a RPC.
	// AKA: Do not edit the speed of projectiles externally on this function.
	public override void shoot(Character character, int[] args) {
		MegamanX mmx = character as MegamanX ?? throw new NullReferenceException();

		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (chargeLevel < 3) {
			int type = 0;
			if (player.input.isHeld(Control.Up, player)) {
				type = 1;
			}
			bubblesOnField.Add(
				new BubbleSplashProj(type, pos, xDir, mmx, player, player.getNextActorNetId(), rpc: true)
			);
		} else if (chargeLevel >= 3) {
			if (mmx.chargedBubbles.Count >= 5) {
				mmx.specialBuster.shoot(character, [3, 1]);
				freeAmmoNextCharge = true;
			} else {
				mmx.popAllBubbles();
				for (int i = 0; i < 6; i++) {
					var bubble = new BubbleSplashProjCharged(
						pos, xDir, mmx, player, i, 
						player.getNextActorNetId(true), true
					);
					mmx.chargedBubbles?.Add(bubble);	
				}
			}
		}
	}
}

public class BubbleSplashProj : Projectile {
	int size;
	float randY;
	float randT;
	int randColor;

	public BubbleSplashProj(
		int type, Point pos, int xDir, Actor owner, Player player, ushort? netId,
		int? size = null, int? randX = null, int? randY = null, float? randT = null,
		bool rpc = false
	) : base(
		pos, xDir, owner, "bubblesplash_start", netId, player	
	) {
		weapon = BubbleSplash.netWeapon;
		projId = (int)ProjIds.BubbleSplash;
		damager.damage = 0.5f;
		vel = new Point(75 * xDir, 0);
		useGravity = false;

		// RNG shenanigans.
		if (randX == null)
		{
			randX = player.character.deltaPos.x == 0 ?
			Helpers.randomRange(75, 125) : !player.character.isDashing?
			Helpers.randomRange(125, 175) : Helpers.randomRange(175, 250);
		}
		if (randY == null) {
			randY = Helpers.randomRange(75, 125);
		}
		if (randT == null) {
			randT = Helpers.randomRange(75, 125);
		}
		if (size == null) {
			size = Helpers.randomRange(0, 2);
		}
		randColor = Helpers.randomRange(0, spriteVariants.Length - 1);

		// Create variables.
		this.size = size.Value;
		this.randY = (float)randY;
		this.randT = (float)randT / 100f;
		useGravity = false;
		vel.x *= randX.Value / 100f;
		vel.y = 0;
		maxTime = this.randT;

		switch (size) {
			case 0: fadeSprite = "bubblesplash_pop_small";
			break;
			case 1: fadeSprite = "bubblesplash_pop_medium";
			break;
			case 2: fadeSprite = "bubblesplash_pop_large";
			break;
		}
		fadeSound = "bubbleSplashPop";
		fadeOnAutoDestroy = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type, (byte)size, (byte)randX, (byte)randY, (byte)randT);
		}
	}

	public override void update() {
		base.update();
		
		if (sprite.name.Contains("start")) {
			vel.y = 0;
			if (isAnimOver()) {
				changeSprite(spriteVariants[randColor], true);
			}
		} else {
			if (vel.y == 0) { 
				vel.y = -20 * (randY / 100f);
			} 
			if (isUnderwater()) {
				vel.y -= 6f;
			} else {
				vel.y -= 1.5f;
			}
			if (sprite.name.Contains("proj")) {
				switch (size) {
					case 0:
						if (frameIndex > 0) { frameSpeed = 0; }
						break;
					case 1:
						if (frameIndex > 1) { frameSpeed = 0; }
						break;
					case 2:
						if (frameIndex > 2) { frameSpeed = 0; }
						break;
				}
			}
		}
	}

	public static string[] spriteVariants = {
		"bubblesplash_proj1",
		"bubblesplash_proj2",
		"bubblesplash_proj3",
	};

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BubbleSplashProj(
			arg.extraData[0], arg.pos, arg.xDir, arg.owner, arg.player, arg.netId,
			arg.extraData[1], arg.extraData[2], arg.extraData[3], arg.extraData[4]
		);
	}
}

public class BubbleSplashProjCharged : Projectile {
	public MegamanX mmx = null!;
	public float yPos;
	public BubbleSplashProjCharged(
		Point pos, int xDir, Actor owner, Player player, int type, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "bubblesplash_proj1", netId, player	
	) {
		weapon = BubbleSplash.netWeapon;
		damager.damage = 1;
		vel = new Point(75 * xDir, 0);
		useGravity = false;
		fadeSprite = "bubblesplash_pop";

		int randColor = Helpers.randomRange(0, 2);
		if (randColor == 0) changeSprite("bubblesplash_proj2", true);
		if (randColor == 1) changeSprite("bubblesplash_proj3", true);
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		this.time = type * 0.2f + 1;
		sprite.doesLoop = true;
		projId = (int)ProjIds.BubbleSplashCharged;

		isOwnerLinked = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)type });
		}
		if (ownerPlayer?.character != null) {
			ownerActor = ownerPlayer.character;
		} 
		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BubbleSplashProjCharged(
			arg.pos, arg.xDir, arg.owner,
			arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (mmx?.destroyed != false || mmx.currentWeapon is not BubbleSplash) {
			destroySelf();
			return;
		}
		time += Global.spf;
		if (time > 2f) time = 0;

		float x = 20 * MathF.Sin((time - 0.1f) * 5) * xDir;
		yPos = -15 * time;
		Point newPos = mmx.pos.addxy(x + (2 * mmx.xDir), yPos);
		changePos(newPos);

		if (time % 1 > 0.5) {
			zIndex = ZIndex.Background;
		} else {
			zIndex = ZIndex.Actor;
		}
	}

	public override void onDestroy() {
		if (mmx != null) {
			mmx?.chargedBubbles?.Remove(this);
		}
	}
}
