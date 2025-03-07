<asp:TemplateField HeaderText="NOMBRE" SortExpression="NOMAGE">
    <ItemTemplate>
        <asp:Label ID="lblNombre" runat="server" Text='<%# Bind("NOMAGE") %>'></asp:Label>
    </ItemTemplate>
    <EditItemTemplate>
        <asp:TextBox ID="txtNombre" runat="server" Text='<%# Bind("NOMAGE") %>'></asp:TextBox>
    </EditItemTemplate>
</asp:TemplateField>
