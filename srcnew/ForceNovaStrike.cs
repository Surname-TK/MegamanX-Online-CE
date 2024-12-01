using System;

namespace MMXOnline;

public class ForceNovaStrike : Weapon {
	public const float ammoUsage = 14;

	public ForceNovaStrike(Player player) {
		damager = new Damager(player, 3f, 13, 0.5f);
		fireRate = 90;
		index = (int)WeaponIds.ForceNovaStrike;
		weaponBarBaseIndex = 42;
		weaponBarIndex = 36;
		weaponSlotIndex = 95;
		killFeedIndex = 104;
		ammo = 0;
		shootSounds = new string[] { "", "", "", "" };
		drawGrayOnLowAmmo = true;
		drawRoundedDown = true;
		hasCustomAnim = true;
	}
	public override void shoot(Character character, int[] args) {
		Player player = character.player;

		setAttackState(player);
	}

	public void setAttackState(Player player) {
		if (!player.character.ownedByLocalPlayer) {
			return;
		}
		player.character.changeState(new ForceNovaStrikeStart(), true);
	}

	public override float getAmmoUsage(int chargeLevel) {
		return ammoUsage;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return player.character?.flag == null && ammo >= ammoUsage;
	}
}


public class ForceNovaStrikeStart : CharState {
	public ForceNovaStrikeStart() : base("nova_strike_start") {
		superArmor = true;
		enterSound = "land";
	}

	public override void update() {
		base.update();

		if (character.isAnimOver() && character.vel.y >= 0) character.changeState(new ForceNovaStrikeState());
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel.y = -character.getJumpPower() * 0.5f;
		character.xPushVel = character.xDir * 90;
	}
}

public class ForceNovaStrikeState : CharState {
	private int leftOrRight = 1;
	private float speedMultiplier = 1f;

	public ForceNovaStrikeState() : base("nova_strike") {
		immuneToWind = true;
		superArmor = true;
		enterSound = "novaStrikeX4";
	}

	public override void update() {
		base.update();

		if (!character.tryMove(new Point(character.xDir * 350 * leftOrRight, 0), out _)) {
			player.character.changeState(new Idle(), true);
			return;
		}

		if (character.flag != null) {
			player.character.changeState(new Idle(), true);
			return;
		}
		if (stateTime > 0.6f) {
			player.character.changeState(new Idle(), true);
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		player.character.useGravity = false;
		player.character.vel.y = 0;
		player.character.stopCharge();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		player.character.yDir = 1;
		player.character.useGravity = true;
	}
}
