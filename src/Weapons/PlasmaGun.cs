﻿using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class PlasmaGun : AxlWeapon {
	public PlasmaGun(int altFire) : base(altFire) {
		shootSounds = new string[] { "plasmaGun", "plasmaGun", "plasmaGun", "plasmaGun" };
		rateOfFire = 1.5f;
		altFireCooldown = 2f;
		index = (int)WeaponIds.PlasmaGun;
		weaponBarBaseIndex = 36;
		weaponSlotIndex = 56;
		killFeedIndex = 71;
		sprite = "axl_arm_plasmagun";

		if (altFire == 1) {
			altFireCooldown = 0.1f;
			shootSounds[3] = "";
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel == 3) {
			if (altFire == 0) {
				return 7;
			}
			return 0.5f;
		}
		return 4;
	}

	public override float whiteAxlFireRateMod() {
		return 2;
	}

	public override void axlGetProjectile(
		Weapon weapon, Point bulletPos, int xDir, Player player, float angle,
	 	IDamagable? target, Character? headshotTarget, Point cursorPos, int chargeLevel, ushort netId
	) {
		if (!player.ownedByLocalPlayer) return;
		if (player?.character is not Axl axl) return;

		Point? bulletDir = Point.createFromAngle(angle);
		Projectile? bullet = null;
		if (chargeLevel < 3) {
			new Anim(bulletPos, "plasmagun_effect", 1, player.getNextActorNetId(), true, sendRpc: true) {
				angle = angle,
				host = player.character
			};
			bullet = new PlasmaGunProj(weapon, bulletPos, xDir, player, bulletDir.Value, netId, sendRpc: true);
			RPC.playSound.sendRpc(shootSounds[0], player.character?.netId);
		} else {
			if (altFire == 0) {
				new VoltTornadoProj(weapon, player.character.pos, xDir, player, netId, sendRpc: true);
				RPC.playSound.sendRpc(shootSounds[3], player.character?.netId);
			} else {
				if (axl.plasmaGunAltProj == null) {
					axl.plasmaGunAltProj = new PlasmaGunAltProj(weapon, bulletPos, cursorPos, 1, player, netId, sendRpc: true);
				}
				return;
			}
		}
	}
}

public class PlasmaGunProj : Projectile {
	Axl? axl;
	float dist;
	public PlasmaGunProj(
		Weapon weapon, Point pos, int xDir, Player player,
		Point bulletDir, ushort netProjId, bool sendRpc = false
	) : base(
		weapon, pos, 1, 600, 2, player, "plasmagun_proj",
		Global.halfFlinch, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.PlasmaGun;
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		destroyOnHit = false;
		maxTime = 0.125f;
		axl = (player.character as Axl);

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;

		isMelee = true;
		if (player?.character != null) {
			owningActor = player.character;
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (axl == null) return;
		dist += speed * Global.spf;
		Point newPos = axl.getAxlBulletPos().add(axl.getAxlBulletDir().times(dist));
		changePos(newPos);
	}
}

public class PlasmaGunAltProj : Projectile {
	Player player;
	const float range = 100;
	float soundCooldown;
	bool hasTarget;
	SoundWrapper? sound;
	public PlasmaGunAltProj(
		Weapon weapon, Point pos, Point cursorPos, int xDir, Player player, ushort netProjId, bool sendRpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "plasmagun_alt_proj",
		Global.miniFlinch, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.PlasmaGun2;

		destroyOnHit = false;
		shouldVortexSuck = false;
		shouldShieldBlock = false;
		this.player = player;

		if (player.character is Axl axl && player.character != null) {
			axl.nonOwnerAxlBulletPos = pos;
			if (axl.isWhiteAxl() == true) {
				projId = (int)ProjIds.PlasmaGun2Hyper;
			}
		}

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		canBeLocal = false;

		isMelee = true;
		if (player?.character != null) {
			owningActor = player.character;
		}
	}

	public override void postUpdate() {
		base.postUpdate();

		Helpers.decrementTime(ref soundCooldown);
		if (soundCooldown == 0) {
			sound = player?.character?.playSound("plasmaGunAlt");
			soundCooldown = 2.259f;
		}

		if (!ownedByLocalPlayer) return;

		Axl? axl = player?.character as Axl;
		if (axl == null || axl.destroyed == true) {
			destroySelf();
			return;
		}

		Point bulletDir = axl.getAxlBulletDir();
		Point bulletPos = axl.getAxlBulletPos();
		float closestAngle = float.MaxValue;
		Character? closestEnemy = null;
		foreach (var player in Global.level.players) {
			Character enemy = player.character;
			if (enemy == null) continue;
			if (!enemy.canBeDamaged(owner.alliance, owner.id, projId)) continue;
			if (enemy.isStealthy(owner.alliance)) continue;
			if (bulletPos.distanceTo(enemy.getCenterPos()) > range) continue;
			Point dirToEnemy = bulletPos.directionToNorm(enemy.getCenterPos());
			float ang = bulletDir.angleWith(dirToEnemy);
			if (ang > 45) continue;
			if (!Global.level.noWallsInBetween(bulletPos, enemy.getCenterPos())) continue;
			if (ang < closestAngle) {
				closestAngle = ang;
				closestEnemy = enemy;
			}
		}

		if (closestEnemy == null) {
			Point destPos = axl.getAxlHitscanPoint(range);
			var hits = Global.level.raycastAll(bulletPos, destPos, new List<Type>() { typeof(Actor), typeof(Wall) });

			CollideData? closestHit = null;
			float bestDist = float.MaxValue;
			foreach (var hit in hits) {
				if (hit.gameObject is Wall && hit.hitData.hitPoint != null) {
					float dist = bulletPos.distanceTo(hit.hitData.hitPoint.Value);
					if (dist < bestDist) {
						bestDist = dist;
						closestHit = hit;
					}
				}
			}

			if (closestHit != null  && closestHit.hitData.hitPoint != null) {
				hasTarget = true;
				destPos = closestHit.hitData.hitPoint.Value;
			} else {
				hasTarget = false;
			}
			changePos(destPos);
		} else {
			hasTarget = true;
			changePos(closestEnemy.getCenterPos());
		}

		if (Global.level.isSendMessageFrame() && player != null) {
			RPC.syncAxlBulletPos.sendRpc(player.id, bulletPos);
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (sound != null && Global.sounds.Contains(sound)) {
			sound.sound.Stop();
			sound.sound.Dispose();
			Global.sounds.Remove(sound);
			sound = null;
		}
	}

	public List<Point> getNodes() {
		List<Point> nodes = new List<Point>();
		int nodeCount = 8;
		Point origin = player.character.getCenterPos();
		if (player.character is Axl axl) {
			if (ownedByLocalPlayer) {
				origin = axl.getAxlBulletPos();
			} else {
				origin = axl.nonOwnerAxlBulletPos;
			}
		}
		Point dirTo = origin.directionTo(pos).normalize();
		float len = origin.distanceTo(pos);
		Point lastNode = origin;
		for (int i = 0; i <= nodeCount; i++) {
			Point node = i == 0 ? lastNode : lastNode.add(dirTo.times(len / 8));
			Point randNode = node;
			if (i > 0 && (i < nodeCount || !hasTarget)) randNode = node.addRand(10, 10);
			nodes.Add(randNode);
			lastNode = node;
		}
		return nodes;
	}

	public override void render(float x, float y) {
		if (player?.character == null) {
			return;
		}

		var col1 = new Color(74, 78, 221);
		var col2 = new Color(61, 113, 255);
		var col3 = new Color(245, 252, 255);
		if (Global.level.gameMode.isTeamMode && damager.owner.alliance == GameMode.redAlliance) {
			col1 = new Color(221, 78, 74);
			col2 = new Color(255, 113, 61);
			col3 = new Color(255, 245, 240);
		}

		float sin = MathF.Sin(Global.time * 30);
		var nodes = getNodes();

		for (int i = 1; i < nodes.Count; i++) {
			Point startPos = nodes[i - 1];
			Point endPos = nodes[i];
			if (Options.main.lowQualityParticles()) {
				DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col3, 2 + sin, 0, true);
			} else {
				DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col1, 4 + sin, 0, true);
				DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col2, 3 + sin, 0, true);
				DrawWrappers.DrawLine(startPos.x, startPos.y, endPos.x, endPos.y, col3, 2 + sin, 0, true);
			}
		}
	}
}

public class VoltTornadoProj : Projectile {
	Player player;
	public VoltTornadoProj(
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool sendRpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player, "volt_tornado_proj", Global.miniFlinch, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		fadeSprite = "volt_tornado_fade";
		projId = (int)ProjIds.VoltTornado;
		if ((player.character as Axl)?.isWhiteAxl() == true) {
			projId = (int)ProjIds.VoltTornadoHyper;
		}
		shouldShieldBlock = false;
		shouldVortexSuck = false;
		destroyOnHit = false;
		maxTime = 1.5f;
		this.player = player;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void onStart() {
		base.onStart();
		playSound("voltTornado");
	}

	public override void update() {
		base.update();
		if (time > 1f) {
			vel.x = xDir * 300;
		}
	}
}
