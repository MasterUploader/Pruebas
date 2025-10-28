.idle-wrapper {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
  max-width: 460px;
  padding: 8px 4px;
}

.icon-row {
  display: flex;
  justify-content: center;
  margin-top: 4px;
}

.icon-row mat-icon {
  font-size: 40px;
  width: 40px;
  height: 40px;
  opacity: 0.85;
}

.title {
  margin: 8px 0 4px;
  text-align: center;
  font-weight: 600;
}

.subtitle {
  margin: 0 0 6px;
  text-align: center;
  color: rgba(0,0,0,0.7);
}

.countdown {
  text-align: center;
  font-size: 14px;
  margin: 4px 0 8px;
}

mat-progress-bar {
  height: 6px;
  border-radius: 3px;
}

.actions {
  display: flex;
  justify-content: center;
  gap: 10px;
  margin-top: 8px;
}

.btn-continue mat-icon,
.btn-logout mat-icon {
  margin-right: 6px;
}



const ref = this.dialog.open(IdleWarningComponent, {
  width: '420px',
  disableClose: true,
  data: { seconds: Math.floor(this.WARNING_BEFORE_CLOSE_MS / 1000) }
});

ref.afterClosed().subscribe(result => {
  if (result === 'continue') {
    // reiniciar timers + keepAlive
  } else if (result === 'logout') {
    // forzar logout
  }
});
