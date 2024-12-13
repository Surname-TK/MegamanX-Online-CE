using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class AimingLaser : Weapon {

	public static AimingLaser netWeapon = new();

	public AimingLaser() : base() {
		index = (int)WeaponIds.AimingLaser;
		fireRate = 60;
		weaponSlotIndex = 128;
        weaponBarBaseIndex = 77;
        weaponBarIndex = 66;
		shootSounds = new string[] {"","","",""};
		weaknessIndex = (int)WeaponIds.SoulBody;
		/* damage = "1";
		hitcooldown = "0.3";
		Flinch = "0";
		FlinchCD = "0";
		effect = "Focuses scanned enemies."; */
	}

	public override bool canShoot(int chargeLevel, Player player) {
		MegamanX? mmx = player.character as MegamanX;

		if (chargeLevel < 3) return base.canShoot(chargeLevel, player) && mmx?.aLaserTargets.Count > 0;

		return base.canShoot(chargeLevel, player);
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel < 3) return 0;
		return base.getAmmoUsage(chargeLevel);
	}
	
	public override void shoot(Character character, int[] args) {
		int chargeLevel = args[0];
		Point pos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		MegamanX mmx = player.character as MegamanX ?? throw new NullReferenceException();
		
		if (chargeLevel < 3) {
			int type = 0;

			foreach(var targ in mmx.aLaserTargets) {
				if (targ.pos.distanceTo(pos) <= 320) {
					new AimingLaserProj(this, pos, xDir, player, type, player.getNextActorNetId(), targ, true);
					addAmmo(-1, player);
					type++;
				}
			}
		} else {
			float angle = xDir > 0 ? 0 : 128;
			new AimingLaserChargedProj(this, pos, xDir, player, angle, player.getNextActorNetId(), true);

			mmx.aLaserTargets.Clear();
		}

		mmx.aLaserCursor?.destroySelf();
		mmx.aLaserCursor = null!;
	}
}


public class AimingLaserTargetAnim : Anim {

	Character chara;

	public AimingLaserTargetAnim(
		Point pos, int xDir, ushort? netId, Character chara
	) : base (
		pos, "aiming_laser_cursor", xDir, netId, false, true
	) {
		this.chara = chara;
	}

	public override void update() {
		base.update();

		changePos(chara.getCenterPos());

		if (sprite.loopCount >= 5) destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		chara.aLaserTargetAnim = null!;
	}
}


public class AimingLaserHud : Anim {

	MegamanX mmx = null!;
	Player player;
	float ang = -64;
	float finalAng;
	const float distance = 64;
	
	public AimingLaserHud(
		Point pos, int xDir, ushort? netId, Player player, int frame
	) : base(
		pos, "aiming_laser_hud", xDir, netId, false
	) {
		frameSpeed = 0;
		frameIndex = frame;
		this.player = player;
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		mmx.aLaserHud = this;
		ang = ang + (frame * 12.8f);

		finalAng = xDir > 0 ? ang : -ang + 128;
		float posX = mmx.getCenterPos().x + (distance * Helpers.cosb(finalAng));
		float posY = mmx.getCenterPos().y + (distance * Helpers.sinb(finalAng));
		changePos(new Point(posX, posY));
	}

	public override void update() {
		base.update();

		if (player.weapon is not AimingLaser) destroySelf();

		xDir = mmx.getShootXDir();
		finalAng = xDir > 0 ? ang : -ang + 128;

		float posX = mmx.getCenterPos().x + (distance * Helpers.cosb(finalAng));
		float posY = mmx.getCenterPos().y + (distance * Helpers.sinb(finalAng));

		changePos(new Point(posX, posY));
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.aLaserHud = null!;
	}
}


public class AimingLaserCursor : Projectile {

	MegamanX mmx = null!;
	Player player;
	float ogAngle = 0;
	float laserAngle;
	const float laserDistance = 64;

	public AimingLaserCursor(
		Weapon weapon, Point pos, int xDir, Player player, ushort? netProjId, bool rpc = false
	) : base (
		AimingLaser.netWeapon, pos, xDir, 0, 0, player, 
		"aiming_laser_cursor", 0, 0, netProjId, player.ownedByLocalPlayer
	) {
		this.player = player;
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		mmx.aLaserCursor = this;
		setIndestructableProperties();

		laserAngle = xDir > 0 ? 0 : 128;
		changePos(mmx.getCenterPos().add(Point.createFromByteAngle(laserAngle).times(laserDistance)));
	}

	public override void update() {
		base.update();

		if (player.weapon is not AimingLaser) destroySelf();

		int dirY = player.input.getYDir(player);
		if (dirY != 0) ogAngle += dirY * 8;

		if (ogAngle < -64) ogAngle = -64;
		if (ogAngle > 64) ogAngle = 64;

		laserAngle = mmx.getShootXDir() > 0 ? ogAngle : -ogAngle + 128;

		float posX = mmx.getCenterPos().x + (laserDistance * Helpers.cosb(laserAngle));
		float posY = mmx.getCenterPos().y + (laserDistance * Helpers.sinb(laserAngle));

		changePos(new Point(posX, posY));
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		var chr = other.gameObject as Character;

		if (chr != null && 
			mmx.aLaserTargets.Count < 3 &&
			chr.canBeDamaged(player.alliance, player.id, projId)) {

				if (!mmx.aLaserTargets.Any(c => c == chr)) {
					mmx.aLaserTargets.Add(chr);
					//chr.addALaserAttacker(mmx);
					if (chr.aLaserTargetAnim == null) {
						chr.aLaserTargetAnim = new AimingLaserTargetAnim(
							chr.getCenterPos(), 1, null, chr
						);
					}
					playSound("axlTarget");
				}

		}
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.aLaserCursor = null!;
	}
}


public class AimingLaserProj : Projectile {
	
	Point endPos;
	Character target = null!;
	MegamanX mmx;
	int type;
	float angDif = 5;
	float length;
	float l;
	float ang;

	public AimingLaserProj(
		Weapon weapon, Point pos, int xDir, Player player, int type,
		ushort? netProjId, Character target = null!, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player,
		"empty", 0, 0.34f, netProjId,
		player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.AimingLaser;
		destroyOnHit = false;
		maxTime = 1f;
		setIndestructableProperties();
		this.target = target;
		this.type = type;
		//endPos = target.pos. ?? target.getCenterPos();
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		//mmx.aLaserProj = this;
		mmx.aLasers.Add(this);

		setEndPos(endPos);

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type  };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}

		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new AimingLaserProj(
			AimingLaser.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer || target == null) return;

		if (target.charState is Die || mmx.player.weapon is not AimingLaser) {
			//mmx.aLaserTargets.Remove(target);
			destroySelf();
		} 
	}

	public override void postUpdate() {
		base.postUpdate();
		if (target == null || target.destroyed) return;

		changePos(mmx.getShootPos());
		endPos = target.getCenterPos();
		setEndPos(endPos);
		length = pos.distanceTo(endPos);
		l = MathF.Max(length - 24, 0);
		ang = pos.directionTo(endPos).byteAngle;
	}


	/* public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;

		var damagable = other.gameObject as Character;
		if (damagable == null || damagable != target) return;

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) && 
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {

				playSound("axlTarget");
				damagable.applyDamage(1, mmx.player, this, (int)WeaponIds.AimingLaser, projId);
				
			}
		} 
	} */

	public void setEndPos(Point end) {
		this.endPos = end;

		if (!ownedByLocalPlayer) {
			changePos(mmx.getShootPos());
			endPos = end;
			//setEndPos(endPos);
			length = pos.distanceTo(endPos);
			l = MathF.Max(length - 24, 0);
			ang = pos.directionTo(endPos).byteAngle;
		} 

		globalCollider = new Collider(getPoints(), true, null!, false, false, 0, Point.zero);
	}

	List<Point> getPoints() {
		Point pointA = pos.add(Point.createFromByteAngle(ang - angDif).times(l));
		Point pointB = pos.add(Point.createFromByteAngle(ang + angDif).times(l));

		return new List<Point>() {
			pos,
			pointA,
			endPos,
			pointB
		};
	} 

	public override void render(float x, float y) {
		base.render(x,y);
		if (destroyed) return;
		
		var colors = new List<Color>()
		{
			new Color(39, 255, 39, 255),
			new Color(255, 39, 42, 255),
			new Color(251, 255, 39, 255),
		};

		DrawWrappers.DrawPolygon(getPoints(), colors[type], true, ZIndex.Actor);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		customData.AddRange(BitConverter.GetBytes(endPos.x));
		customData.AddRange(BitConverter.GetBytes(endPos.y));

		return customData;
	}
	public override void updateCustomActorNetData(byte[] data) {
		float endX = BitConverter.ToSingle(data[0..4], 0);
		float endY = BitConverter.ToSingle(data[4..8], 0);

		setEndPos(new Point(endX, endY));
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.aLaserTargets.Remove(target);
		mmx.aLasers.Remove(this);
		mmx.shootAnimTime = 0;
	}
}


public class AimingLaserChargedProj : Projectile {

	MegamanX mmx = null!;
	float ang = 0;
	float finalAng;
	float length = 0;
	Player player;
	Point endPos;
	Point shootDir;
	float l;
	float angDif = 15;

	public AimingLaserChargedProj(
		Weapon weapon, Point pos, int xDir, Player player,
		float byteAngle, ushort? netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 1, player,
		"empty", 0, 0.3f, netProjId,
		player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.AimingLaserCharged;
		maxTime = 3f;
		setIndestructableProperties();
		mmx = player.character as MegamanX ?? throw new NullReferenceException();
		mmx.aLaserChargedProj = this;
		releasePlasma = player.hasPlasma();
		this.byteAngle = byteAngle;
		this.player = player;
		shootDir = new Point(xDir, 0);
		endPos = pos.add(shootDir.normalize().times(length));

		setEndPos(endPos);

		if (rpc) {
			rpcCreateByteAngle(pos, player, netProjId, byteAngle);
		}

		canBeLocal = false;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new AimingLaserChargedProj(
			AimingLaser.netWeapon, arg.pos, arg.xDir, 
			arg.player, arg.byteAngle, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (length < 128) length += 2;
		l = MathF.Max(length * 0.75f, 0);

		int dirY = player.input.getYDir(player);
		if (dirY != 0) ang += dirY * 8;

		if (ang < -64) ang = -64;
		if (ang > 64) ang = 64;

		byteAngle = mmx.getShootXDir() > 0 ? ang : -ang + 128;

		if (!ownedByLocalPlayer) return;
		if (player.weapon is not AimingLaser) destroySelf();
	}

	public override void postUpdate() {
		base.postUpdate();
		if (byteAngle == null) return;
		if (!ownedByLocalPlayer) return;

		changePos(mmx.getShootPos());
		endPos = pos.add(Point.createFromByteAngle(byteAngle.Value) * length);
		setEndPos(endPos);
	}

	public void setEndPos(Point endPos) {
		this.endPos = endPos;

		globalCollider = new Collider(getPoints(), true, null!, false, false, HitboxFlag.Hitbox, Point.zero);
	}

	List<Point> getPoints() {
		if (byteAngle == null) return new List<Point>();
		Point pointA = pos.add(Point.createFromByteAngle(byteAngle.Value - angDif).times(l));
		Point pointB = pos.add(Point.createFromByteAngle(byteAngle.Value + angDif).times(l));

		return new List<Point>() {
			pos,
			pointA,
			endPos,
			pointB
		};
	} 

	public override void render(float x, float y) {
		base.render(x,y);
		var color = new Color(39, 255, 230, 255);

		DrawWrappers.DrawPolygon(getPoints(), color, true, ZIndex.Actor);
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = new();

		customData.AddRange(BitConverter.GetBytes(endPos.x));
		customData.AddRange(BitConverter.GetBytes(endPos.y));

		return customData;
	}
	public override void updateCustomActorNetData(byte[] data) {
		float endX = BitConverter.ToSingle(data[0..4], 0);
		float endY = BitConverter.ToSingle(data[4..8], 0);

		setEndPos(new Point(endX, endY));
	}

	public override void onDestroy() {
		base.onDestroy();
		mmx.aLaserChargedProj = null!;
		mmx.shootAnimTime = 0;
	}
}
