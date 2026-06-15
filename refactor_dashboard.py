import re

with open('e:/Quality diagnostics center/LabSystem.UI/Views/DashboardView.xaml', 'r', encoding='utf-8') as f:
    dashboard_content = f.read()

with open('e:/Quality diagnostics center/old_dashboard.xaml', 'r', encoding='utf-8') as f:
    old_dashboard_content = f.read()

# Extract Work Queue tab from old_dashboard
work_queue_start_idx = old_dashboard_content.find('<!-- ─── TAB 3: WORK QUEUE (3-COLUMN SINGLE-SCREEN) ─── -->')
if work_queue_start_idx == -1:
    work_queue_start_idx = old_dashboard_content.find('<!-- ─── TAB 3: WORK QUEUE')
    
# Find the end of TabItem for Work Queue. 
tab_item_end = old_dashboard_content.find('</TabItem>', work_queue_start_idx) + len('</TabItem>')
work_queue_tab = old_dashboard_content[work_queue_start_idx:tab_item_end]

# Inject Amend button into Work Queue Tab
# We will inject it into the Flag column or Status column.
# The original has: <DataGridTemplateColumn Header="Flag" Width="80" IsReadOnly="True">
# We can replace the CellTemplate of the Flag column with the one including the Amend button.
# Let's search for the Flag column cell template
flag_template_search = """<DataGridTemplateColumn.CellTemplate>
                                            <DataTemplate>
                                                <Border CornerRadius="10" Padding="6 3" HorizontalAlignment="Center" Margin="0 4"
                                                        Visibility="{Binding IsAbnormal, Converter={StaticResource BoolToVis}}">
                                                    <Border.Background>
                                                        <SolidColorBrush Color="#FFCDD2"/>
                                                    </Border.Background>
                                                    <TextBlock Text="ABNORMAL" Foreground="#C62828" FontWeight="Bold" FontSize="10"/>
                                                </Border>
                                            </DataTemplate>
                                        </DataGridTemplateColumn.CellTemplate>"""

flag_template_replace = """<DataGridTemplateColumn.CellTemplate>
                                            <DataTemplate>
                                                <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                                    <Border CornerRadius="10" Padding="6 3" Margin="0 4"
                                                            Visibility="{Binding IsAbnormal, Converter={StaticResource BoolToVis}}">
                                                        <Border.Background>
                                                            <SolidColorBrush Color="#FFCDD2"/>
                                                        </Border.Background>
                                                        <TextBlock Text="ABNORMAL" Foreground="#C62828" FontWeight="Bold" FontSize="10"/>
                                                    </Border>
                                                    <!-- Amend Button for Completed Results -->
                                                    <Button Command="{Binding DataContext.AmendResultCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" 
                                                            CommandParameter="{Binding}"
                                                            Visibility="{Binding IsReadOnly, Converter={StaticResource BoolToVis}}"
                                                            Style="{StaticResource MaterialDesignFlatButton}"
                                                            Padding="4" Margin="4 0 0 0" Height="24"
                                                            Foreground="#1565C0" ToolTip="Amend Result">
                                                        <materialDesign:PackIcon Kind="Pencil" Width="14" Height="14"/>
                                                    </Button>
                                                </StackPanel>
                                            </DataTemplate>
                                        </DataGridTemplateColumn.CellTemplate>"""

work_queue_tab = work_queue_tab.replace(flag_template_search, flag_template_replace)

# Now extract the Unified Queue Tab from DashboardView.xaml
unified_queue_start_idx = dashboard_content.find('<!-- ─── TAB 3: WORK QUEUE (UNIFIED MASTER-DETAIL) ─── -->')
if unified_queue_start_idx == -1:
    unified_queue_start_idx = dashboard_content.find('<!-- ─── TAB 3: UNIFIED QUEUE')

unified_queue_end = dashboard_content.find('<!-- ─── TAB 4: APPOINTMENTS ─── -->')
if unified_queue_end == -1:
    unified_queue_end = dashboard_content.find('</TabItem>', unified_queue_start_idx) + len('</TabItem>')
    # Also grab leading whitespaces for the next tab
    
# Slice the content
new_dashboard_content = dashboard_content[:unified_queue_start_idx] + work_queue_tab + '\n\n            ' + dashboard_content[unified_queue_end:]

with open('e:/Quality diagnostics center/LabSystem.UI/Views/DashboardView.xaml', 'w', encoding='utf-8') as f:
    f.write(new_dashboard_content)

print("DashboardView.xaml updated successfully.")
