/* Footer visualmente igual, pero SIN position: fixed */
.footer {
  background: #bd0909;            /* tu color */
  color: #fff;
  height: 40px;                   /* ajusta a la altura que usabas */
  padding: 0 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  text-align: center;

  border-top: 1px solid rgba(0, 0, 0, 0.12);  /* separador sutil */
  box-shadow: 0 -1px 0 rgba(0, 0, 0, 0.06);   /* opcional: línea superior */
  
  flex: 0 0 auto;                 /* clave: que no intente crecer en flex */
}

/* texto interno opcional */
.footer__text {
  font-size: 0.95rem;
  line-height: 1;
  white-space: nowrap;
}




<footer class="footer">
  <div class="footer__text">
    Impresión Vertical Tarjeta de Débito &nbsp;—&nbsp; Davivienda © {{ currentYear }}
  </div>
</footer>



